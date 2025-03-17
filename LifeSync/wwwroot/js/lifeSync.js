// wwwroot/js/firebase.js
let lastFirebaseFetchTime = 0;
const FETCH_INTERVAL = 60000;

async function fetchFirebaseData() {
    const now = Date.now();
    if (now - lastFirebaseFetchTime < FETCH_INTERVAL) return;

    try {
        const response = await fetch('/Index?handler=FetchData&source=firebase', { credentials: 'include' });
        if (!response.ok) throw new Error(`Firebase veri çekme başarısız: ${response.status}`);
        const rawData = await response.json();
        const preprocessedData = preprocessTasks(rawData, 'firebase');
        await saveToBackend(preprocessedData, 'firebase');
        lastFirebaseFetchTime = now;
        return preprocessedData;
    } catch (error) {
        console.error('Firebase Hata:', error);
        return null;
    }
}

function startFirebasePolling() {
    setInterval(async () => {
        const data = await fetchFirebaseData();
        if (data) displayData(data, 'firebase');
    }, FETCH_INTERVAL);
}