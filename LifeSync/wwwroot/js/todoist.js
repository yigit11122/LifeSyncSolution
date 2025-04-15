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

async function fetchTodoistTasks() {
    const now = Date.now();
    if (now - lastTodoistFetchTime < FETCH_INTERVAL) return;

    try {
        const tokenRes = await fetch('/api/get-token?source=todoist');
        if (!tokenRes.ok) throw new Error("Access token alınamadı.");
        const { accessToken } = await tokenRes.json();

        const response = await fetch('https://api.todoist.com/rest/v2/tasks', {
            headers: { Authorization: `Bearer ${accessToken}` }
        });

        if (!response.ok) throw new Error(`Todoist API hatası: ${response.status}`);
        const rawTasks = await response.json();

        console.log('Todoist ham veri:', rawTasks);
        console.log("İlk task örneği:", rawTasks[0]);

        const preprocessedTasks = preprocessTasks(rawTasks, 'todoist');
        console.log("preprocess sonrası:", preprocessedTasks);

        await saveToBackend(preprocessedTasks, 'todoist');
        lastTodoistFetchTime = now;

        return preprocessedTasks;
    } catch (error) {
        console.error('Todoist veri çekme hatası:', error.message);
        return null;
    }
}

function startTodoistPolling() {
    setInterval(async () => {
        const tasks = await fetchTodoistTasks();
        if (tasks) displayData(tasks, 'todoist');
    }, FETCH_INTERVAL);
}

document.getElementById("connect-todoist")?.addEventListener("click", initiateTodoistOAuth);
