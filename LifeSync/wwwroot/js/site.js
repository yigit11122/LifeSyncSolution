// FETCH_INTERVAL artık global olarak tanımlı, burada tanımlamıyoruz

async function saveToBackend(pages, source) {
    try {
        const requestBody = {
            Source: source,
            Data: pages.map(page => ({
                Id: page.id,
                Content: page.content,
                CreatedAt: page.createdAt,
                DueDate: null,
                StartDate: null,
                Completed: false
            }))
        };

        const response = await fetch('/api/sync', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(requestBody)
        });

        if (!response.ok) {
            const errorText = await response.text();
            console.error(`Veri kaydetme başarısız: ${response.status} - ${errorText}`);
            throw new Error(errorText);
        }

        const result = await response.json();
        console.log('Veri kaydetme sonucu:', result);
    } catch (error) {
        console.error('Veri kaydetme hatası:', error.message);
    }
}

async function fetchDataFromBackend(source) {
    try {
        const response = await fetch(`/Index?handler=FetchData&source=${source}`, { credentials: 'include' });
        if (!response.ok) {
            throw new Error(`Backend veri çekme başarısız: ${response.status} - ${await response.text()}`);
        }
        const data = await response.json();
        console.log(`${source} için çekilen veriler:`, data);
        return data;
    } catch (error) {
        console.error(`${source} veri çekme hatası:`, error);
        return null;
    }
}

function preprocessTasks(data, source) {
    if (!data || !Array.isArray(data)) {
        console.error(`${source} için geçersiz veri formatı:`, data);
        return [];
    }

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
    } else if (source === 'fitbit') {
        return data.map(activity => ({
            id: activity.id || crypto.randomUUID(),
            content: activity.activityName || 'No Activity',
            createdAt: activity.startTime ? new Date(activity.startTime).toISOString() : new Date().toISOString(),
            source: 'fitbit',
        }));
    } else if (source === 'lifesync') {
        return data.map(item => ({
            id: item.id || crypto.randomUUID(),
            content: item.content || 'No Content',
            createdAt: item.createdAt ? new Date(item.createdAt).toISOString() : new Date().toISOString(),
            source: 'lifesync',
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
    } else if (source === 'fitbit') {
        return {
            activities: data.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt)),
        };
    } else if (source === 'lifesync') {
        return {
            items: data.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt)),
        };
    } else if (source === 'notion') {
        return {
            pages: data.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt)),
        };
    }
    return data;
}

function displayData(data, source) {
    const container = document.getElementById(`${source}-list`);
    if (!container) {
        console.error(`Container '${source}-list' bulunamadı.`);
        return;
    }

    const organized = organizeData(data, source);
    container.innerHTML = '';
    if (source === 'todoist') {
        if (organized.activeTasks.length > 0) {
            container.innerHTML += `<h3>Aktif Görevler</h3><ul>${organized.activeTasks.map(t => `<li>${t.content}</li>`).join('')}</ul>`;
        }
        if (organized.completedTasks.length > 0) {
            container.innerHTML += `<h3>Tamamlanan Görevler</h3><ul>${organized.completedTasks.map(t => `<li>${t.content}</li>`).join('')}</ul>`;
        }
    } else if (source === 'googleCalendar') {
        if (organized.events.length > 0) {
            container.innerHTML += `<h3>Takvim Etkinlikleri</h3><ul>${organized.events.map(e => `<li>${e.content}</li>`).join('')}</ul>`;
        }
    } else if (source === 'notion') {
        if (organized.pages && organized.pages.length > 0) {
            container.innerHTML += `<h3>Notion Sayfaları</h3><ul>${organized.pages.map(p => `<li>${p.content}</li>`).join('')}</ul>`;
        }
    } else if (source === 'fitbit') {
        if (organized.activities.length > 0) {
            container.innerHTML += `<h3>Fitbit Aktiviteleri</h3><ul>${organized.activities.map(a => `<li>${a.content}</li>`).join('')}</ul>`;
        }
    } else if (source === 'lifesync') {
        if (organized.items.length > 0) {
            container.innerHTML += `<h3>LifeSync Öğeleri</h3><ul>${organized.items.map(i => `<li>${i.content}</li>`).join('')}</ul>`;
        }
    } else {
        container.innerHTML = '<p>Veri türü desteklenmiyor.</p>';
    }
    console.log(`${source} verileri ekrana basıldı:`, organized);
}