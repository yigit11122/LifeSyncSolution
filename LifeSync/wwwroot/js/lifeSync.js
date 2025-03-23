let lastLifeSyncFetchTime = 0;

async function fetchLifeSyncData() {
    const now = Date.now();
    if (now - lastLifeSyncFetchTime < FETCH_INTERVAL) return;

    try {
        const response = await fetch('/Index?handler=FetchData&source=lifesync', { credentials: 'include' });
        if (!response.ok) throw new Error(`LifeSync veri çekme başarısız: ${response.status} - ${await response.text()}`);
        const rawData = await response.json();
        console.log('Çekilen LifeSync verileri:', rawData);
        const preprocessedData = preprocessTasks(rawData, 'lifesync');
        await saveToBackend(preprocessedData, 'lifesync');
        lastLifeSyncFetchTime = now;
        return preprocessedData;
    } catch (error) {
        console.error('LifeSync Hata:', error);
        return null;
    }
}

function startLifeSyncPolling() {
    setInterval(async () => {
        const data = await fetchLifeSyncData();
        if (data) displayData(data, 'lifesync');
    }, FETCH_INTERVAL);
}