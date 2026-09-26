// ==========================================================================
// MarketLink — client-side behaviour
// ==========================================================================

// ---- Notification badge: poll the unread count for signed-in users --------
(function () {
    const badge = document.querySelector(".ml-notif-badge");
    if (!badge) return;

    function refresh() {
        fetch("/Notifications/Count")
            .then(r => (r.ok ? r.json() : null))
            .then(data => {
                if (!data) return;
                badge.textContent = data.count;
                badge.classList.toggle("d-none", data.count === 0);
            })
            .catch(() => { /* ignore */ });
    }

    refresh();
    setInterval(refresh, 30000);
})();

// ---- AI assistant widget --------------------------------------------------
(function () {
    const widget = document.getElementById("ai-widget");
    if (!widget) return;

    const toggle = document.getElementById("ai-toggle");
    const panel = document.getElementById("ai-panel");
    const closeBtn = document.getElementById("ai-close");
    const messages = document.getElementById("ai-messages");
    const input = document.getElementById("ai-input");
    const sendBtn = document.getElementById("ai-send");

    toggle.addEventListener("click", () => {
        panel.classList.toggle("d-none");
        if (!panel.classList.contains("d-none")) input.focus();
    });
    closeBtn.addEventListener("click", () => panel.classList.add("d-none"));

    function addMessage(text, isUser) {
        const div = document.createElement("div");
        div.className = "ai-msg " + (isUser ? "ai-user" : "ai-bot");
        div.textContent = text;
        messages.appendChild(div);
        messages.scrollTop = messages.scrollHeight;
        return div;
    }

    async function send() {
        const text = input.value.trim();
        if (!text) return;
        addMessage(text, true);
        input.value = "";

        const thinking = addMessage("...", false);
        try {
            const res = await fetch("/Ai/Chat", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ message: text })
            });
            const data = await res.json();
            thinking.textContent = data.reply || "Sorry, I could not answer that.";
        } catch (e) {
            thinking.textContent = "Sorry, the assistant is unavailable right now.";
        }
    }

    sendBtn.addEventListener("click", send);
    input.addEventListener("keydown", e => { if (e.key === "Enter") send(); });

    document.querySelectorAll(".ai-suggest").forEach(link => {
        link.addEventListener("click", e => {
            e.preventDefault();
            input.value = link.textContent.trim();
            panel.classList.remove("d-none");
            send();
        });
    });
})();

// ---- Leaflet map helper ---------------------------------------------------
// Reads markers from a JSON script tag (#map-data) and renders an
// OpenStreetMap map. Used by the market/map and profile views.
window.initLeafletMap = function (elementId, markers, defaultCenter) {
    const el = document.getElementById(elementId);
    if (!el || typeof L === "undefined") return null;

    const center = (markers && markers.length)
        ? [markers[0].lat, markers[0].lng]
        : (defaultCenter || [12.9716, 77.5946]);

    const map = L.map(elementId).setView(center, 13);

    L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
        maxZoom: 19,
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(map);

    const bounds = [];
    (markers || []).forEach(m => {
        if (m.lat == null || m.lng == null) return;
        const icon = L.divIcon({
            className: "",
            html: `<div style="font-size:1.6rem;color:${m.color || "#2e7d32"}"><i class="bi ${m.icon || "bi-geo-alt-fill"}"></i></div>`,
            iconSize: [26, 26]
        });
        const marker = L.marker([m.lat, m.lng], { icon }).addTo(map);
        let popup = `<strong>${m.title || ""}</strong>`;
        if (m.subtitle) popup += `<br>${m.subtitle}`;
        if (m.link) popup += `<br><a href="${m.link}" target="_blank" rel="noopener">Get directions</a>`;
        marker.bindPopup(popup);
        bounds.push([m.lat, m.lng]);
    });

    if (bounds.length > 1) map.fitBounds(bounds, { padding: [40, 40] });
    return map;
};
