// js/todoist.js
const TODOIST_CLIENT_ID = 'dde975c6776b4b8f98fd4ed5d80bac39'; // Todoist’ten aldığın Client ID
const TODOIST_REDIRECT_URI = 'https://localhost:7263/auth/todoist/callback'; // ZB backend’ine göre ayarlanacak
const TODOIST_SCOPE = 'data:read';
let lastTodoistFetchTime = 0;
const FETCH_INTERVAL = 60000; // 1 dakika

// OAuth akışını başlat (backend üzerinden)
function initiateTodoistOAuth() {
    const state = Math.random().toString(36).substring(2);
    const authUrl = `/auth/todoist/connect?state=${state}`; // Backend’in connect endpoint’ine yönlendirme
    window.location.href = authUrl;
}

// Todoist’ten görevleri çek
async function fetchTodoistTasks() {
    const now = Date.now();
    if (now - lastTodoistFetchTime < FETCH_INTERVAL) return;

    try {
        const response = await fetch('/api/todoist/tasks', {
            method: 'GET',
            credentials: 'include', // Çerezler için
        });

        if (!response.ok) {
            throw new Error(`Todoist API isteği başarısız: ${response.status}`);
        }

        const rawTasks = await response.json();
        const preprocessedTasks = preprocessTasks(rawTasks, 'todoist');
        await saveToBackend(preprocessedTasks, 'todoist');
        lastTodoistFetchTime = now;
        return preprocessedTasks;
    } catch (error) {
        console.error('Todoist Hata:', error);
        return null;
    }
}

// Periyodik çekme
function startTodoistPolling() {
    setInterval(async () => {
        const tasks = await fetchTodoistTasks();
        if (tasks) displayData(tasks, 'todoist');
    }, FETCH_INTERVAL);
}