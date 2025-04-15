//HTML injection'a karşı güvenli gösterim
function escapeHtml(text) {
    const div = document.createElement("div");
    div.textContent = text;
    return div.innerHTML;
}

function removeMarkdown(text) {
    return text
        .replace(/\[([^\]]+)\]\([^)]+\)/g, '$1') 
        .replace(/[_*~`>#-]/g, '')               // özel karakterleri temizle
        .trim();
}

//Veriyi backend'e gönder
async function saveToBackend(items, source) {
    try {
        const requestBody = {
            Source: source,
            Data: items.map(item => {
                const base = {
                    Id: item.id,
                    Content: item.content,
                    CreatedAt: item.createdAt ?? new Date().toISOString(),
                    Source: source
                };

                if (source === "todoist") {
                    base.DueDate = item.dueDate ?? null;
                    base.StartDate = item.startDate ?? null;
                    base.Completed = item.completed ?? false;
                }

                return base;
            })
        };

        console.log("Gönderilen veri:", requestBody);

        const response = await fetch('/api/sync', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(requestBody)
        });

        if (!response.ok) {
            const errorText = await response.text();
            console.error(`Sync başarısız: ${response.status} - ${errorText}`);
            throw new Error(errorText);
        }

        const result = await response.json();
        console.log('Sync başarılı:', result);
    } catch (error) {
        console.error('Sync hatası:', error.message);
    }
}


//Backend'den veri çek
async function fetchDataFromBackend(source) {
    try {
        const response = await fetch(`/Index?handler=FetchData&source=${source}`, { credentials: 'include' });
        if (!response.ok) throw new Error(await response.text());
        const data = await response.json();
        console.log(`${source} verileri alındı:`, data);
        return data;
    } catch (error) {
        console.error(`${source} çekme hatası:`, error);
        return null;
    }
}

//Veriyi normalize et + filtrele + temizle
function preprocessTasks(data, source) {
    if (!Array.isArray(data)) {
        console.error(`${source} verisi geçersiz:`, data);
        return [];
    }

    if (source === 'todoist') {
        return data
            .filter(task => task.is_completed === false) // sadece aktif görevler
            .map(task => ({
                id: task.id,
                content: removeMarkdown(task.content || 'No Title'),
                dueDate: task.due?.date ? new Date(task.due.date).toISOString() : null,
                completed: false,
                createdAt: task.created_at ? new Date(task.created_at).toISOString() : new Date().toISOString(),
                source: 'todoist'
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

    // Diğer kaynaklar 
    return data;
}

//Verileri türüne göre gruplandır
function organizeData(data, source) {
    if (source === 'todoist') {
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

//Verileri HTML'e bas
function displayData(data, source) {
    const container = document.getElementById(`${source}-list`);
    if (!container) {
        console.error(`📛 '${source}-list' bulunamadı.`);
        return;
    }

    const organized = organizeData(data, source);
    container.innerHTML = '';

    if (source === 'todoist') {
        if (organized.activeTasks.length > 0) {
            container.innerHTML += `<h3>Aktif Görevler</h3><ul>${organized.activeTasks.map(t => `<li>${escapeHtml(t.content)}</li>`).join('')}</ul>`;
        }
        if (organized.completedTasks.length > 0) {
            container.innerHTML += `<h3>Tamamlanan Görevler</h3><ul>${organized.completedTasks.map(t => `<li>${escapeHtml(t.content)}</li>`).join('')}</ul>`;
        }
    }

    if (source === 'notion') {
        if (organized.pages?.length > 0) {
            container.innerHTML += `<h3>Notion Sayfaları</h3><ul>${organized.pages.map(p => `<li>${escapeHtml(p.content)}</li>`).join('')}</ul>`;
        }
    }

    console.log(`${source} verileri ekrana basıldı:`, organized);
}
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
            Data: [
                {
                    Id: crypto.randomUUID(),
                    Content: content,
                    CreatedAt: new Date().toISOString(),
                    DueDate: null,
                    StartDate: null,
                    Completed: false
                }
            ]
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
