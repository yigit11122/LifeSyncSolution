const FITBIT_CLIENT_ID = '23Q5HN';
const FITBIT_REDIRECT_URI = 'https://localhost:7263/auth/fitbit/callback';
const FITBIT_SCOPE = 'activity';
let lastFitbitFetchTime = 0;

function initiateFitbitOAuth() {
    console.log('Fitbit OAuth başlatılıyor...');
    const state = Math.random().toString(36).substring(2);
    const authUrl = `/auth/fitbit/connect?state=${state}`;
    window.location.href = authUrl;
}

async function fetchFitbitData() {
    const now = Date.now();
    if (now - lastFitbitFetchTime < FETCH_INTERVAL) return;

    try {
        const response = await fetch('/Index?handler=FetchData&source=fitbit', { credentials: 'include' });
        if (!response.ok) throw new Error(`Fitbit veri çekme başarısız: ${response.status} - ${await response.text()}`);
        const rawData = await response.json();
        console.log('Çekilen Fitbit verileri:', rawData);
        const preprocessedData = preprocessTasks(rawData, 'fitbit');
        await saveToBackend(preprocessedData, 'fitbit');
        lastFitbitFetchTime = now;
        return preprocessedData;
    } catch (error) {
        console.error('Fitbit Hata:', error);
        return null;
    }
}

function startFitbitPolling() {
    setInterval(async () => {
        const data = await fetchFitbitData();
        if (data) displayData(data, 'fitbit');
    }, FETCH_INTERVAL);
}