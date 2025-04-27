// HTML güvenli gösterim
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

async function saveLifeSyncTask(content) {
    const task = {
        id: crypto.randomUUID(),
        content: content,
        createdAt: new Date().toISOString(),
        completed: false,
        dueDate: null,
        startDate: null
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


document.addEventListener("DOMContentLoaded", () => {
    loadLifeSyncNotes();
    loadLifeSyncTasks();

    // Not ekleme
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

    // Görev ekleme
    const taskBtn = document.getElementById("lifesync-task-submit");
    const taskInput = document.getElementById("lifesync-task-input");
    if (taskBtn && taskInput) {
        taskBtn.addEventListener("click", () => {
            const content = taskInput.value.trim();
            if (content) {
                saveLifeSyncTask(content);
                taskInput.value = '';
            }
        });
    }
});
