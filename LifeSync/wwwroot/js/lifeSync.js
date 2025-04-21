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
        console.log("Not kaydedildi:", note);
        await loadLifeSyncNotes();
    } catch (err) {
        console.error("Not kaydedilemedi:", err.message);
    }
}

async function loadLifeSyncNotes() {
    try {
        const res = await fetch("/api/lifesync/data");
        if (!res.ok) throw new Error(await res.text());
        const notes = await res.json();

        const container = document.getElementById("lifesync-list");
        container.innerHTML = `<h3>Benim Notlarım</h3><ul>${notes.map(n => `<li>${n.content}</li>`).join('')}</ul>`;
    } catch (err) {
        console.error("Notlar yüklenemedi:", err.message);
    }
}

document.getElementById("lifesync-submit").addEventListener("click", () => {
    const input = document.getElementById("lifesync-input");
    const content = input.value.trim();
    if (!content) return;

    saveLifeSyncNote(content);
    input.value = '';
});

document.addEventListener("DOMContentLoaded", loadLifeSyncNotes);
