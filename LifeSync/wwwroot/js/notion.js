// js/notion.js
const NOTION_CLIENT_ID = '1b9d872b-594c-807f-bf84-0037fed59b9c';
const NOTION_REDIRECT_URI = 'https://localhost:7263/auth/notion/callback';
const NOTION_SCOPE = 'read';
let lastNotionFetchTime = 0;
const FETCH_INTERVAL = 60000;

// OAuth akışını başlat
function initiateNotionOAuth() {
    const state = Math.random().toString(36).substring(2);
    const authUrl = `/auth/notion/connect?state=${state}`;
    window.location.href = authUrl;
}

// Notion’dan sayfaları çek
async function fetchNotionPages() {
    const now = Date.now();
    if (now - lastNotionFetchTime < FETCH_INTERVAL) return;

    try {
        const response = await fetch('/api/notion/pages', {
            method: 'GET',
            credentials: 'include',
        });

        if (!response.ok) {
            throw new Error(`Notion API isteği başarısız: ${response.status}`);
        }

        const rawPages = await response.json();
        const preprocessedPages = preprocessTasks(rawPages, 'notion');
        await saveToBackend(preprocessedPages, 'notion');
        lastNotionFetchTime = now;
        return preprocessedPages;
    } catch (error) {
        console.error('Notion Hata:', error);
        return null;
    }
}

// Periyodik çekme
function startNotionPolling() {
    setInterval(async () => {
        const pages = await fetchNotionPages();
        if (pages) displayData(pages, 'notion');
    }, FETCH_INTERVAL);
}