const TODOIST_CLIENT_ID = 'dde975c6776b4b8f98fd4ed5d80bac39';
const TODOIST_REDIRECT_URI = 'https://localhost:7263/auth/todoist/callback';
const TODOIST_SCOPE = 'data:read';
let lastTodoistFetchTime = 0;

function initiateTodoistOAuth() {
    console.log('Todoist OAuth başlatılıyor...');
    const state = Math.random().toString(36).substring(2);
    const authUrl = `/auth/todoist/connect?state=${state}`;
    window.location.href = authUrl;
}

async function fetchTodoistTasks() {  //Veri çek
    const now = Date.now();
    if (now - lastTodoistFetchTime < FETCH_INTERVAL) return;

    try {
        const response = await fetch('/Index?handler=FetchData&source=todoist', { credentials: 'include' });
        if (!response.ok) throw new Error(`Todoist veri çekme başarısız: ${response.status} - ${await response.text()}`);
        const rawTasks = await response.json();
        console.log('Çekilen Todoist verileri:', rawTasks);
        const preprocessedTasks = preprocessTasks(rawTasks, 'todoist');
        await saveToBackend(preprocessedTasks, 'todoist');
        lastTodoistFetchTime = now;
        return preprocessedTasks;
    } catch (error) {
        console.error('Todoist Hata:', error);
        return null;
    }
}

function startTodoistPolling() {  //Döngü
    setInterval(async () => {
        const tasks = await fetchTodoistTasks();
        if (tasks) displayData(tasks, 'todoist');
    }, FETCH_INTERVAL);
}