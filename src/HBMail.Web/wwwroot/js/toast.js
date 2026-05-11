// Pure JS Toast — runs in browser, no Blazor state dependency
window.HBToast = (function () {
    const ICONS = {
        success: 'bi-check-circle-fill',
        error: 'bi-exclamation-octagon-fill',
        warning: 'bi-exclamation-triangle-fill',
        info: 'bi-info-circle-fill'
    };

    function ensureContainer() {
        let c = document.getElementById('hb-toast-container');
        if (!c) {
            c = document.createElement('div');
            c.id = 'hb-toast-container';
            c.className = 'hb-toast-container';
            c.setAttribute('aria-live', 'polite');
            c.setAttribute('aria-atomic', 'true');
            document.body.appendChild(c);
        }
        return c;
    }

    function dismiss(card) {
        if (!card || card.dataset.dismissing === '1') return;
        card.dataset.dismissing = '1';
        card.classList.remove('hb-toast-in');
        card.classList.add('hb-toast-out');
        setTimeout(() => { if (card.parentNode) card.parentNode.removeChild(card); }, 300);
    }

    function show(type, title, message, durationMs) {
        const container = ensureContainer();
        const t = (type || 'info').toLowerCase();
        const icon = ICONS[t] || ICONS.info;
        const dur = durationMs || 4000;

        const card = document.createElement('div');
        card.className = 'hb-toast-card hb-toast-' + t + ' hb-toast-in';
        card.setAttribute('role', 'alert');
        card.style.setProperty('--toast-duration', dur + 'ms');

        const safeTitle = (title || '').replace(/</g, '&lt;');
        const safeMsg = (message || '').replace(/</g, '&lt;');
        const msgHtml = safeMsg ? `<span class="hb-toast-message">${safeMsg}</span>` : '';

        card.innerHTML = `
            <div class="hb-toast-icon"><i class="bi ${icon}"></i></div>
            <div class="hb-toast-body">
                <strong class="hb-toast-title">${safeTitle}</strong>
                ${msgHtml}
            </div>
            <button type="button" class="hb-toast-close" aria-label="إغلاق"><i class="bi bi-x-lg"></i></button>
            <div class="hb-toast-progress"></div>
        `;

        // Click handlers
        card.addEventListener('click', () => dismiss(card));
        card.querySelector('.hb-toast-close').addEventListener('click', (e) => {
            e.stopPropagation();
            dismiss(card);
        });

        container.appendChild(card);

        // Auto-dismiss
        setTimeout(() => dismiss(card), dur);
    }

    return { show: show };
})();
