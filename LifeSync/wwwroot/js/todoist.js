const TODOIST_CLIENT_ID = 'dde975c6776b4b8f98fd4ed5d80bac39';
const TODOIST_REDIRECT_URI = 'https://localhost:7263/auth/todoist/callback';
const TODOIST_SCOPE = 'data:read_write'; // Sadece veritabanı için yeterli ama ileride genişletilebilir

let lastTodoistFetchTime = 0;

function initiateTodoistOAuth() {
    const state = Math.random().toString(36).substring(2);
    const authUrl = `/auth/todoist/connect?state=${state}&scope=${encodeURIComponent(TODOIST_SCOPE)}`;
    window.location.href = authUrl;
}

async function fetchTodoistTasks() {
    const now = Date.now();
    if (now - lastTodoistFetchTime < (window.FETCH_INTERVAL || 60000)) return;

    try {
        const tokenRes = await fetch('/api/get-token?source=todoist');
        if (!tokenRes.ok) throw new Error("Access token alınamadı.");
        const { accessToken } = await tokenRes.json();

        const response = await fetch('https://api.todoist.com/rest/v2/tasks', {
            headers: { Authorization: `Bearer ${accessToken}` }
        });

        if (!response.ok) throw new Error(`Todoist API hatası: ${response.status}`);
        const rawTasks = await response.json();

        const preprocessedTasks = preprocessTasks(rawTasks, 'todoist');
        await saveToBackend(preprocessedTasks, 'todoist');
        lastTodoistFetchTime = now;

        return preprocessedTasks;
    } catch (error) {
        console.error('Todoist veri çekme hatası:', error.message);
        return null;
    }
}

// ✅ SADECE veritabanında tamamlandı olarak işaretle
async function handleTodoistCheckboxChange(id) {
    try {
        const res = await fetch(`/api/todoist/complete/${id}`, { method: 'PUT' });
        if (!res.ok) throw new Error(await res.text());

        console.log(`✅ Görev veritabanında tamamlandı: ${id}`);

        // ⚡ Anında gösterimi güncelle
        const updated = await fetchDataFromBackend("todoist");
        if (updated) {
            // DOM güncellenmeden önce kısa bir ara ver (render için)
            setTimeout(() => displayData(updated, "todoist"), 50);
        }
    } catch (err) {
        console.error("Veritabanında işaretleme hatası:", err.message);
    }
}

// Bağlan butonu
document.getElementById("connect-todoist")?.addEventListener("click", initiateTodoistOAuth);
