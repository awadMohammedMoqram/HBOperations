// بنك حضرموت - Browser Notifications helper
window.hbNotifications = {
    permissionRequested: false,

    async requestPermission() {
        if (!('Notification' in window)) return 'unsupported';
        if (Notification.permission === 'granted') return 'granted';
        if (Notification.permission === 'denied') return 'denied';
        try {
            const result = await Notification.requestPermission();
            this.permissionRequested = true;
            return result;
        } catch {
            return 'error';
        }
    },

    show(title, body, transactionId) {
        if (!('Notification' in window)) return false;
        if (Notification.permission !== 'granted') return false;
        try {
            const n = new Notification(title || 'بنك حضرموت', {
                body: body || '',
                icon: '/Images/aboutLogo-fDwVp-So.svg',
                badge: '/Images/aboutLogo-fDwVp-So.svg',
                lang: 'ar',
                dir: 'rtl',
                tag: transactionId ? ('tx-' + transactionId) : undefined,
                renotify: !!transactionId
            });
            n.onclick = () => {
                window.focus();
                if (transactionId) {
                    window.location.href = '/transactions/' + transactionId;
                }
                n.close();
            };
            // auto close after 8s
            setTimeout(() => { try { n.close(); } catch { } }, 8000);
            return true;
        } catch {
            return false;
        }
    }
};
