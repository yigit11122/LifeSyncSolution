// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Google Calendar API Key (kendi anahtarını koy!)
const API_KEY = "AIzaSyD...xyz"; // Google Cloud’dan al!
const CALENDAR_ID = "primary"; // Kullanıcının ana takvimi

function getCalendarData() {
    document.getElementById("events").innerHTML = "Görevler yükleniyor...";

    axios.get(`https://www.googleapis.com/calendar/v3/calendars/${CALENDAR_ID}/events`, {
        params: {
            key: API_KEY,
            timeMin: new Date().toISOString(), // Bugünden sonrası
            maxResults: 10 // Maksimum 10 etkinlik
        }
    })
        .then(response => {
            const events = response.data.items;
            if (!events || events.length === 0) {
                document.getElementById("events").innerHTML = "Takviminizde etkinlik bulunamadı.";
                return;
            }

            let eventList = "<h3>Çekilen Görevler:</h3><ul>";
            events.forEach(event => {
                const title = event.summary || "Başlıksız Görev";
                const start = event.start.dateTime || event.start.date;
                eventList += `<li>${title} - ${start}</li>`;
            });
            eventList += "</ul>";

            document.getElementById("events").innerHTML = eventList;
        })
        .catch(error => {
            document.getElementById("events").innerHTML = "Hata: " + error.message;
        });
}