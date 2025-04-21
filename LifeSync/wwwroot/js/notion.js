let lastNotionFetchTime = 0;

function initiateNotionOAuth() {
    console.log('Notion OAuth başlatılıyor...');
    const state = Math.random().toString(36).substring(2);
    const authUrl = `/auth/notion/connect?state=${state}`;
    window.location.href = authUrl;
}

async function fetchNotionPages() {
    const now = Date.now();
    if (now - lastNotionFetchTime < window.FETCH_INTERVAL) {
        console.log('Notion veri çekme için bekleme süresi: FETCH_INTERVAL');
        return;
    }

    try {
        console.log('Notion veri çekme isteği gönderiliyor...');
        const response = await fetch('/api/notion/fetch', { credentials: 'include' });

        if (!response.ok) {
            const errorText = await response.text();
            console.error(`Notion veri çekme başarısız: ${response.status} - ${errorText}`);
            throw new Error(errorText);
        }

        const rawPages = await response.json();
        console.log('Çekilen Notion verileri:', rawPages);

        if (rawPages.error) {
            throw new Error(rawPages.error);
        }

        const preprocessedPages = preprocessNotionPages(rawPages.data);
        console.log('Notion verileri işlendikten sonra:', preprocessedPages);

        if (preprocessedPages.length === 0) {
            console.log('Notion verileri boş, kaydetme işlemi yapılmayacak.');
            return;
        }

        await saveToBackend(preprocessedPages, 'notion');
        lastNotionFetchTime = now;
        return preprocessedPages;
    } catch (error) {
        console.error('Notion Hata:', error.message);
        document.getElementById("notion-list").innerHTML = `Hata: ${error.message}`;
        return null;
    }
}

function preprocessNotionPages(data) {
    const parsedData = JSON.parse(data);
    const pages = parsedData.results || [];

    const processedPages = pages.map(page => {
        let title = "Başlık Yok";
        if (page.properties && page.properties.Name && page.properties.Name.title && page.properties.Name.title.length > 0) {
            title = page.properties.Name.title[0].text.content;
        }

        const createdAt = page.created_time || new Date().toISOString();
        const id = page.id ? page.id.replace(/-/g, '') : crypto.randomUUID();

        return {
            id: id,
            content: title,
            createdAt: createdAt,
            source: 'notion'
        };
    });

    console.log('İşlenmiş Notion verileri:', processedPages);
    return processedPages;
}

document.getElementById("connect-notion").addEventListener("click", initiateNotionOAuth);
