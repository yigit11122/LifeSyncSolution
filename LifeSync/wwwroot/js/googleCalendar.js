const GOOGLE_CLIENT_ID = '2798359091-a9k9hnik9k1k7hbc2s31o23cqc57l013.apps.googleusercontent.com';
const GOOGLE_REDIRECT_URI = 'https://localhost:7263/auth/google/callback';
const GOOGLE_SCOPE = 'https://www.googleapis.com/auth/calendar.readonly';
let lastGoogleFetchTime = 0;

function initiateGoogleCalendarOAuth() {
    console.log('Google Calendar OAuth başlatılıyor...');
    const state = Math.random().toString(36).substring(2);
    const authUrl = `/auth/google/connect?state=${state}`;
    window.location.href = authUrl;
}

async function fetchGoogleCalendarEvents() {
    const now = Date.now();
    if (now - lastGoogleFetchTime < FETCH_INTERVAL) return;

    try {
        const response = await fetch('/Index?handler=FetchData&source=googleCalendar', { credentials: 'include' });
        if (!response.ok) throw new Error(`Google Calendar veri çekme başarısız: ${response.status} - ${await response.text()}`);
        const rawEvents = await response.json();
        console.log('Çekilen Google Calendar verileri:', rawEvents);
        const preprocessedEvents = preprocessTasks(rawEvents, 'googleCalendar');
        await saveToBackend(preprocessedEvents, 'googleCalendar');
        lastGoogleFetchTime = now;
        return preprocessedEvents;
    } catch (error) {
        console.error('Google Calendar Hata:', error);
        return null;
    }
}

function startGoogleCalendarPolling() {
    setInterval(async () => {
        const events = await fetchGoogleCalendarEvents();
        if (events) displayData(events, 'googleCalendar');
    }, FETCH_INTERVAL);
}

//TheEnd