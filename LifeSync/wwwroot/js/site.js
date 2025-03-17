// js/site.js
// Veriyi backend’e kaydet
async function saveToBackend(data, source) {
    try {
        const response = await fetch('/api/sync', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ source, data }),
            credentials: 'include',
        });
        if (!response.ok) {
            throw new Error(`Backend kaydetme başarısız: ${response.status}`);
        }
    } catch (error) {
        console.error('Hata:', error);
    }
}

// Veriyi backend’den çek
async function fetchDataFromBackend(source) {
    try {
        const response = await fetch(`/api/data/${source}`, {
            credentials: 'include',
        });
        if (!response.ok) {
            throw new Error(`Backend veri çekme başarısız: ${response.status}`);
        }
        return await response.json();
    } catch (error) {
        console.error(`${source} veri çekme hatası:`, error);
        return null;
    }
}

// Veri ön işleme
function preprocessTasks(data, source) {
    if (source === 'todoist') {
        return data.map(task => ({
            id: task.id,
            content: task.content || 'No Title',
            dueDate: task.due?.date ? new Date(task.due.date).toISOString() : null,
            completed: task.completed || false,
            source: 'todoist',
        }));
    } else if (source === 'googleCalendar') {
        return data.map(event => ({
            id: event.id,
            content: event.summary || 'No Title',
            startDate: event.start?.dateTime ? new Date(event.start.dateTime).toISOString() : null,
            source: 'googleCalendar',
        }));
    } else if (source === 'notion') {
        return data.map(page => ({
            id: page.id,
            content: page.properties?.title?.title[0]?.text?.content || 'No Title',
            createdAt: page.created_time ? new Date(page.created_time).toISOString() : null,
            source: 'notion',
        }));
    } else if (source === 'lifeSync') {
        return data.map(item => ({
            id: item.id || crypto.randomUUID(),
            content: item.content || 'No Content',
            createdAt: item.createdAt ? new Date(item.createdAt).toISOString() : new Date().toISOString(),
            source: 'lifeSync',
        }));
    }
    return data;
}

// Veri akışlarını organize et
function organizeData(data, source) {
    if (source === 'todoist') {
        return {
            activeTasks: data.filter(item => !item.completed),
            completedTasks: data.filter(item => item.completed),
        };
    } else if (source === 'googleCalendar') {
        return {
            events: data.sort((a, b) => new Date(a.startDate) - new Date(b.startDate)),
        };
    } else if (source === 'notion') {
        return {
            pages: data.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt)),
        };
    } else if (source === 'lifeSync') {
        return {
            items: data.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt)),
        };
    }
    return data;
}

// Verileri ekranda göster
function displayData(data, source) {
    const container = document.getElementById(`${source}-list`);
    if (!container) return;

    const organized = organizeData(data, source);
    if (source === 'todoist') {
        container.innerHTML = `
            <h3>Aktif Görevler</h3>
            <ul>${organized.activeTasks.map(t => `<li>${t.content}</li>`).join('')}</ul>
            <h3>Tamamlanan Görevler</h3>
            <ul>${organized.completedTasks.map(t => `<li>${t.content}</li>`).join('')}</ul>
        `;
    } else if (source === 'googleCalendar') {
        container.innerHTML = `
            <h3>Takvim Etkinlikleri</h3>
            <ul>${organized.events.map(e => `<li>${e.content}</li>`).join('')}</ul>
        `;
    } else if (source === 'notion') {
        container.innerHTML = `
            <h3>Notion Sayfaları</h3>
            <ul>${organized.pages.map(p => `<li>${p.content}</li>`).join('')}</ul>
        `;
    } else if (source === 'lifeSync') {
        container.innerHTML = `
            <h3>LifeSync Öğeleri</h3>
            <ul>${organized.items.map(i => `<li>${i.content}</li>`).join('')}</ul>
        `;
    }
}