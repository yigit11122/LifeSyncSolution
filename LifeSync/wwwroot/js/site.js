// HTML injection'a karşı güvenli gösterim
function escapeHtml(text) {
    const div = document.createElement("div");
    div.textContent = text;
    return div.innerHTML;
}

// Markdown'ı temizle
function removeMarkdown(text) {
    return text
        .replace(/\[([^\]]+)\]\([^)]+\)/g, '$1')
        .replace(/[_*~>#-]/g, '')
        .trim();
}

// Local tarih inputunu UTC'ye çevir
function toUtcISOStringFromLocalInput(localDateStr) {
    const local = new Date(localDateStr);
    return new Date(local.getTime() - local.getTimezoneOffset() * 60000).toISOString();
}

// Veriyi backend'e gönder
async function saveToBackend(items, source) {
    try {
        const requestBody = {
            Source: source,
            Data: items.map(item => {
                const base = {
                    Id: item.id,
                    Content: item.content,
                    CreatedAt: new Date(item.createdAt ?? Date.now()).toISOString(),
                    Source: source
                };

                if (source === "todoist" || source === "lifesync-task") {
                    base.DueDate = item.dueDate ? toUtcISOStringFromLocalInput(item.dueDate) : null;
                    base.StartDate = item.startDate ? toUtcISOStringFromLocalInput(item.startDate) : null;
                    base.Completed = item.completed ?? false;
                }

                return base;
            })
        };

        const response = await fetch('/api/sync', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(requestBody)
        });

        if (!response.ok) throw new Error(await response.text());
        console.log('Sync başarılı:', await response.json());
    } catch (error) {
        console.error('Sync hatası:', error.message);
    }
}

// Backend'den veri çek
async function fetchDataFromBackend(source) {
    try {
        const response = await fetch(`/Index?handler=FetchData&source=${source}`, { credentials: 'include' });
        if (!response.ok) throw new Error(await response.text());
        const data = await response.json();
        console.log(`${source} verileri alındı:`, data);
        return data;
    } catch (error) {
        console.error(`${source} veri çekme hatası:`, error);
        return null;
    }
}

// Sadece veritabanı için tamamla
async function markTaskAsCompleted(id) {
    try {
        const res = await fetch(`/api/todoist/complete/${id}`, { method: 'PUT' });
        if (!res.ok) throw new Error(await res.text());
        console.log(`Görev ${id} tamamlandı (veritabanı)`);

        const updated = await fetchDataFromBackend("todoist");
        if (updated) displayData(updated, "todoist");
    } catch (err) {
        console.error("Tamamlama hatası:", err.message);
    }
}

// Görevleri işle
function preprocessTasks(data, source) {
    if (!Array.isArray(data)) {
        console.error(`${source} verisi geçersiz:`, data);
        return [];
    }

    if (source === 'todoist' || source === 'lifesync-task') {
        return data.map(task => ({
            id: task.id,
            content: removeMarkdown(task.content || 'No Title'),
            dueDate: task.dueDate || task.due?.date || null,
            startDate: task.startDate || null,
            completed: task.completed ?? false,
            createdAt: task.createdAt || task.created_at || new Date().toISOString(),
            source: source
        }));
    }

    if (source === 'notion') {
        return data.map(page => ({
            id: page.id,
            content: removeMarkdown(page.content || 'No Content'),
            createdAt: page.createdAt ? new Date(page.createdAt).toISOString() : new Date().toISOString(),
            source: 'notion'
        }));
    }

    return data;
}

// Verileri grupla
function organizeData(data, source) {
    if (source === 'todoist' || source === 'lifesync-task') {
        return {
            activeTasks: data.filter(t => !t.completed),
            completedTasks: data.filter(t => t.completed)
        };
    }

    if (source === 'notion') {
        return { pages: data.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt)) };
    }

    return data;
}

// Gösterim
function displayData(data, source) {
    const container = document.getElementById(`${source}-list`);
    if (!container) return;

    const organized = organizeData(data, source);
    container.innerHTML = '';

    if (source === 'todoist' || source === 'lifesync-task') {
        if (organized.activeTasks.length > 0) {
            const listHtml = organized.activeTasks.map(t => `
                <li>
                    <input type="checkbox" onchange="markTaskAsCompleted('${t.id}')" />
                    <span>${escapeHtml(t.content)}</span>
                    ${t.startDate ? `<br/><small>⏰ Hatırlatma: ${new Date(t.startDate).toLocaleString()}</small>` : ''}
                </li>
            `).join('');
            container.innerHTML += `<h3>Aktif Görevler</h3><ul>${listHtml}</ul>`;
        }

        if (organized.completedTasks.length > 0) {
            const listHtml = organized.completedTasks.map(t => `
                <li><span style="text-decoration: line-through; color: #888;">${escapeHtml(t.content)}</span></li>
            `).join('');
            container.innerHTML += `<h3>Tamamlanan Görevler</h3><ul>${listHtml}</ul>`;
        }
    }

    if (source === 'notion') {
        if (organized.pages?.length > 0) {
            container.innerHTML += `<h3>Notion Notların</h3><ul>${organized.pages.map(p => `<li>${escapeHtml(p.content)}</li>`).join('')}</ul>`;
        }
    }

    if (source === 'lifesync') {
        if (Array.isArray(organized) && organized.length > 0) {
            container.innerHTML += `<h3>Benim Notlarım</h3><ul>${organized.map(n => `<li>${escapeHtml(n.content)}</li>`).join('')}</ul>`;
        } else {
            container.innerHTML = "<p>Henüz not yok.</p>";
        }
    }
}

// Yeni not kaydet
async function saveNewNote() {
    const content = document.getElementById("note-content").value.trim();
    const status = document.getElementById("note-status");

    if (!content) {
        status.innerText = "Lütfen boş not girmeyin.";
        return;
    }

    try {
        const requestBody = {
            Source: "lifesync",
            Data: [{
                Id: crypto.randomUUID(),
                Content: content,
                CreatedAt: new Date().toISOString(),
                DueDate: null,
                StartDate: null,
                Completed: false
            }]
        };

        const response = await fetch('/api/sync', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(requestBody)
        });

        if (!response.ok) throw new Error(await response.text());

        status.innerText = "Not kaydedildi!";
        document.getElementById("note-content").value = "";
    } catch (err) {
        console.error("Not kaydetme hatası:", err.message);
        status.innerText = "Kaydetme sırasında hata oluştu.";
    }
}
