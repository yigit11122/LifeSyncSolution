// wwwroot/js/site.js
async function saveToBackend(data, source) {
    try {
        const response = await fetch('/api/sync', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ source, data }),
            credentials: 'include',
        });
        if (!response.ok) throw new Error(`Backend kaydetme başarısız: ${response.status}`);
    } catch (error) {
        console.error('Hata:', error);
    }
}

async function fetchDataFromBackend(source) {
    try {
        const response = await fetch(`/Index?handler=FetchData&source=${source}`, { credentials: 'include' });
        if (!response.ok) throw new Error(`Backend veri çekme başarısız: ${response.status}`);
        return await response.json();
    } catch (error) {
        console.error(`${source} veri çekme hatası:`, error);
        return null;
    }
}

function preprocessTasks(data, source) {
    if (source === 'todoist') {
        return data.map(task => ({
            id: task.id,
            content: task.content || 'No Title',
            dueDate: task.dueDate ? new Date(task.dueDate).toISOString() : null,
            completed: task.completed || false,
            source: 'todoist',
        }));
    } else if (source === 'googleCalendar') {
        return data.map(event => ({
            id: event.id,
            content: event.summary || 'No Title',
            startDate: event.startDate ? new Date(event.startDate).toISOString() : null,
            source: 'googleCalendar',
        }));
    } else if (source === 'notion') {
        return data.map(page => ({
            id: page.id,
            content: page.content || 'No Title',
            createdAt: page.createdAt ? new Date(page.createdAt).toISOString() : null,
            source: 'notion',
        }));
    } else if (source === 'firebase') {
        return data.map(item => ({
            id: item.id || crypto.randomUUID(),
            content: item.content || 'No Content',
            createdAt: item.createdAt ? new Date(item.createdAt).toISOString() : new Date().toISOString(),
            source: 'firebase',
        }));
    } else if (source === 'fitbit') {
        return data.map(activity => ({
            id: activity.id || crypto.randomUUID(),
            content: activity.activityName || 'No Activity',
            createdAt: activity.startTime ? new Date(activity.startTime).toISOString() : new Date().toISOString(),
            source: 'fitbit',
        }));
    }
    return data;
}

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
    } else if (source === 'firebase') {
        return {
            items: data.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt)),
        };
    } else if (source === 'fitbit') {
        return {
            activities: data.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt)),
        };
    }
    return data;
}

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
    } else if (source === 'firebase') {
        container.innerHTML = `
            <h3>LifeSync Öğeleri</h3>
            <ul>${organized.items.map(i => `<li>${i.content}</li>`).join('')}</ul>
        `;
    } else if (source === 'fitbit') {
        container.innerHTML = `
            <h3>Fitbit Aktiviteleri</h3>
            <ul>${organized.activities.map(a => `<li>${a.content}</li>`).join('')}</ul>
        `;
    }
}