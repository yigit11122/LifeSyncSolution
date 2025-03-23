const NOTION_CLIENT_ID = '1b9d872b-594c-807f-bf84-0037fed59b9c';
const NOTION_REDIRECT_URI = 'https://localhost:7263/auth/notion/callback';
const NOTION_SCOPE = 'read';
let lastNotionFetchTime = 0;

function initiateNotionOAuth() {
    console.log('Notion OAuth başlatılıyor...');
    const state = Math.random().toString(36).substring(2);
    const authUrl = `/auth/notion/connect?state=${state}`;
    window.location.href = authUrl;
}

async function fetchNotionPages() {
    const now = Date.now();
    if (now - lastNotionFetchTime < FETCH_INTERVAL) return;

    try {
        const response = await fetch('/Index?handler=FetchData&source=notion', { credentials: 'include' });
        if (!response.ok) throw new Error(`Notion veri çekme başarısız: ${response.status} - ${await response.text()}`);
        const rawPages = await response.json();
        console.log('Çekilen Notion verileri:', rawPages);
        const preprocessedPages = preprocessTasks(rawPages, 'notion');
        await saveToBackend(preprocessedPages, 'notion');
        lastNotionFetchTime = now;
        return preprocessedPages;
    } catch (error) {
        console.error('Notion Hata:', error);
        return null;
    }
}

function startNotionPolling() {
    setInterval(async () => {
        const pages = await fetchNotionPages();
        if (pages) displayData(pages, 'notion');
    }, FETCH_INTERVAL);
}