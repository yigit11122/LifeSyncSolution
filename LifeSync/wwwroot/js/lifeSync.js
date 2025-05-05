function escapeHtml(text) {
    const div = document.createElement("div");
    div.textContent = text;
    return div.innerHTML;
}

////================== NOTLAR ==================////
async function saveLifeSyncNote(content) {
    const note = {
        id: crypto.randomUUID(),
        content: content,
        createdAt: new Date().toISOString()
    };

    const requestBody = {
        Source: "lifesync",
        Data: [note]
    };

    try {
        const res = await fetch("/api/sync", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(requestBody)
        });

        if (!res.ok) throw new Error(await res.text());
        console.log("✅ Not kaydedildi:", note);
        await loadLifeSyncNotes();
    } catch (err) {
        console.error("❌ Not kaydedilemedi:", err.message);
    }
}

async function loadLifeSyncNotes() {
    try {
        const res = await fetch("/api/lifesync/data");
        if (!res.ok) throw new Error(await res.text());
        const notes = await res.json();

        const container = document.getElementById("lifesync-list");
        if (!container) return;

        container.innerHTML = `<h3>Benim Notlarım</h3><ul>${notes.map(n =>
            `<li>${escapeHtml(n.content)}</li>`).join('')}</ul>`;
    } catch (err) {
        console.error("❌ Notlar yüklenemedi:", err.message);
    }
}

////================== GÖREVLER ==================////
async function saveLifeSyncTask(content, reminderDateTime = null) {
    const task = {
        id: crypto.randomUUID(),
        content: content,
        createdAt: new Date().toISOString(),
        completed: false,
        dueDate: null,
        startDate: reminderDateTime ? new Date(reminderDateTime).toISOString() : null
    };

    const requestBody = {
        Source: "lifesync-task",
        Data: [task]
    };

    try {
        const res = await fetch("/api/sync", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(requestBody)
        });

        if (!res.ok) throw new Error(await res.text());
        console.log("✅ Görev kaydedildi:", task);
        await loadLifeSyncTasks();
    } catch (err) {
        console.error("❌ Görev kaydedilemedi:", err.message);
    }
}

async function markLifeSyncTaskCompleted(id) {
    try {
        const res = await fetch(`/api/todoist/complete/${id}`, { method: "PUT" });
        if (!res.ok) throw new Error(await res.text());

        console.log("✅ LifeSync görevi tamamlandı:", id);
        await loadLifeSyncTasks();
    } catch (err) {
        console.error("❌ Görev tamamlama hatası:", err.message);
    }
}

async function loadLifeSyncTasks() {
    try {
        const res = await fetch("/api/lifesync-task/data");
        if (!res.ok) throw new Error(await res.text());
        const tasks = await res.json();

        const container = document.getElementById("lifesync-task-list");
        if (!container) return;

        const active = tasks.filter(t => !t.completed);
        const done = tasks.filter(t => t.completed);

        container.innerHTML = "";

        if (active.length > 0) {
            container.innerHTML += `<h3>Aktif Görevler</h3><ul>${active.map(t =>
                `<li>
                    <input type="checkbox" onchange="markLifeSyncTaskCompleted('${t.id}')">
                    <span>${escapeHtml(t.content)}</span>
                    ${t.startDate ? `<small><br/>⏰ Hatırlatma: ${new Date(t.startDate).toLocaleString()}</small>` : ""}
                </li>`
            ).join('')}</ul>`;
        }

        if (done.length > 0) {
            container.innerHTML += `<h3>Tamamlanan Görevler</h3><ul>${done.map(t =>
                `<li><span style="text-decoration: line-through; color: #888;">${escapeHtml(t.content)}</span></li>`
            ).join('')}</ul>`;
        }
    } catch (err) {
        console.error("❌ Görevler yüklenemedi:", err.message);
    }
}

////================== BİLDİRİM KONTROLÜ ==================////
const shownReminderIds = new Set();

async function checkRemindersAndNotify() {
    try {
        const res = await fetch("/api/reminders/data");
        if (!res.ok) throw new Error(await res.text());
        const reminders = await res.json();

        const now = new Date();
        const fiveMinutesAgo = new Date(now.getTime() - 5 * 60000);

        reminders.forEach(rem => {
            const scheduled = new Date(rem.scheduledAt); // dikkat: backend ISO string döndürmeli
            if (
                scheduled > fiveMinutesAgo &&
                scheduled <= now &&
                !shownReminderIds.has(rem.id)
            ) {
                showNotification("⏰ Görev Hatırlatıcısı", rem.title || "Bir görevin var!");
                shownReminderIds.add(rem.id);
                console.log("🔔 Bildirim gösterildi:", rem.title, scheduled.toLocaleString());
            }
        });
    } catch (err) {
        console.error("🔔 Hatırlatıcı kontrol hatası:", err.message);
    }
}

function showNotification(title, body) {
    if (Notification.permission === "granted") {
        new Notification(title, { body });
    }
}

function requestNotificationPermission() {
    if ("Notification" in window && Notification.permission !== "granted") {
        Notification.requestPermission().then(permission => {
            if (permission === "granted") {
                console.log("🔔 Bildirim izni verildi.");
            } else {
                console.warn("❌ Bildirim izni reddedildi.");
            }
        });
    }
}

////================== BAŞLATICI ==================////
document.addEventListener("DOMContentLoaded", () => {
    loadLifeSyncNotes();
    loadLifeSyncTasks();
    requestNotificationPermission();
    setInterval(checkRemindersAndNotify, 60000);

    const noteBtn = document.getElementById("lifesync-submit");
    const noteInput = document.getElementById("lifesync-input");
    if (noteBtn && noteInput) {
        noteBtn.addEventListener("click", () => {
            const content = noteInput.value.trim();
            if (content) {
                saveLifeSyncNote(content);
                noteInput.value = '';
            }
        });
    }

    const taskBtn = document.getElementById("lifesync-task-submit");
    const taskInput = document.getElementById("lifesync-task-input");
    const reminderInput = document.getElementById("lifesync-task-reminder");
    if (taskBtn && taskInput) {
        taskBtn.addEventListener("click", () => {
            const content = taskInput.value.trim();
            const reminder = reminderInput?.value;
            if (content) {
                saveLifeSyncTask(content, reminder);
                taskInput.value = '';
                if (reminderInput) reminderInput.value = '';
            }
        });
    }
});
