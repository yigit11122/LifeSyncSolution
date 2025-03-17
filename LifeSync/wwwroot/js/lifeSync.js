// js/lifeSync.js
let lastLifeSyncFetchTime = 0;
const FETCH_INTERVAL = 60000;

// LifeSync veritabanından verileri çek
async function fetchLifeSyncData() {
    const now = Date.now();
    if (now - lastLifeSyncFetchTime < FETCH_INTERVAL) return;

    try {
        const response = await fetch('/api/lifeSync/data', {
            method: 'GET',
            credentials: 'include',
        });

        if (!response.ok) {
            throw new Error(`LifeSync API isteği başarısız: ${response.status}`);
        }

        const rawData = await response.json();
        const preprocessedData = preprocessTasks(rawData, 'lifeSync');
        await saveToBackend(preprocessedData, 'lifeSync');
        lastLifeSyncFetchTime = now;
        return preprocessedData;
    } catch (error) {
        console.error('LifeSync Hata:', error);
        return null;
    }
}

// Periyodik çekme
function startLifeSyncPolling() {
    setInterval(async () => {
        const data = await fetchLifeSyncData();
        if (data) displayData(data, 'lifeSync');
    }, FETCH_INTERVAL);
}