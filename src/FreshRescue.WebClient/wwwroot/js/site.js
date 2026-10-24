// Tiện ích JS dùng chung cho mọi trang WebClient — gọi qua window.FR.
// Chủ sở hữu: Đức Anh (hạ tầng WebClient, 07-WORK-BREAKDOWN.md §12.3).
window.FR = (function () {
    'use strict';

    const body = document.body;
    const csrfToken = document.querySelector('meta[name="csrf-token"]')?.content;
    const moneyFormat = new Intl.NumberFormat('vi-VN');
    // API trả giờ UTC (OQ-33); hiển thị theo giờ Việt Nam dù máy người xem đặt múi giờ nào.
    const timeFormat = new Intl.DateTimeFormat('vi-VN', { hour: '2-digit', minute: '2-digit', timeZone: 'Asia/Ho_Chi_Minh' });
    const dayFormat = new Intl.DateTimeFormat('vi-VN', { day: '2-digit', month: '2-digit', timeZone: 'Asia/Ho_Chi_Minh' });

    function goToLogin() {
        const returnUrl = location.pathname + location.search;
        location.href = `${body.dataset.loginUrl}?returnUrl=${encodeURIComponent(returnUrl)}`;
    }

    /**
     * fetch tới controller của WebClient (không gọi thẳng API — token nằm trong cookie HttpOnly).
     * Trả { ok, status, data }; lỗi có data = { status, errorCode, message, errors } (ApiExceptionFilter).
     * 401 thì chuyển sang trang đăng nhập.
     */
    async function api(url, options = {}) {
        const method = options.method || 'GET';
        const headers = { 'X-Requested-With': 'XMLHttpRequest', 'Accept': 'application/json' };
        if (options.body !== undefined) {
            headers['Content-Type'] = 'application/json';
        }
        if (method !== 'GET' && csrfToken) {
            headers['RequestVerificationToken'] = csrfToken;
        }

        let response;
        try {
            response = await fetch(url, {
                method,
                headers,
                credentials: 'same-origin',
                body: options.body === undefined ? undefined : JSON.stringify(options.body),
            });
        } catch {
            return { ok: false, status: 0, data: { message: 'Mất kết nối mạng, vui lòng thử lại.' } };
        }

        if (response.status === 401) {
            goToLogin();
            return new Promise(() => { });
        }

        const data = await response.json().catch(() => ({}));
        if (!response.ok && !data.message) {
            data.message = response.status === 403
                ? 'Bạn không có quyền thực hiện thao tác này. Chức năng mua hàng chỉ dành cho tài khoản khách hàng.'
                : 'Đã có lỗi xảy ra, vui lòng thử lại.';
        }
        return { ok: response.ok, status: response.status, data };
    }

    function formatMoney(amount) {
        return moneyFormat.format(amount) + '₫';
    }

    function formatTime(iso) {
        return timeFormat.format(new Date(iso));
    }

    function formatWindow(startIso, endIso) {
        return `${formatTime(startIso)}–${formatTime(endIso)} ${dayFormat.format(new Date(startIso))}`;
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text ?? '';
        return div.innerHTML;
    }

    /** Vẽ nguyên chuỗi token thành QR — không cắt, không định dạng lại (03-API-CONTRACT.md §4.3). */
    function renderQr(element, token) {
        if (typeof qrcode !== 'function') {
            element.textContent = token;
            return;
        }
        const qr = qrcode(0, 'M');
        qr.addData(token);
        qr.make();
        element.innerHTML = qr.createSvgTag({ cellSize: 4, margin: 2, scalable: true, alt: 'Mã QR nhận hàng' });
    }

    /** Đếm ngược cho mọi phần tử [data-countdown="<ISO UTC>"]; hết giờ thì bắn sự kiện fr:expired. */
    function startCountdowns(root = document) {
        root.querySelectorAll('[data-countdown]').forEach((element) => {
            if (element.dataset.countdownStarted) {
                return;
            }
            element.dataset.countdownStarted = 'true';
            const deadline = new Date(element.dataset.countdown).getTime();

            const tick = () => {
                const left = Math.max(0, Math.floor((deadline - Date.now()) / 1000));
                const minutes = String(Math.floor(left / 60)).padStart(2, '0');
                const seconds = String(left % 60).padStart(2, '0');
                element.textContent = left > 0 ? `${minutes}:${seconds}` : 'đã hết hạn';
                if (left === 0) {
                    clearInterval(timer);
                    element.dispatchEvent(new CustomEvent('fr:expired', { bubbles: true }));
                }
            };
            const timer = setInterval(tick, 1000);
            tick();
        });
    }

    return {
        api,
        goToLogin,
        formatMoney,
        formatTime,
        formatWindow,
        escapeHtml,
        renderQr,
        startCountdowns,
        isAuthenticated: body.dataset.authenticated === 'true',
        isCustomer: body.dataset.customer === 'true',
    };
})();

FR.startCountdowns();
document.querySelectorAll('[data-qr-token]').forEach((element) => FR.renderQr(element, element.dataset.qrToken));
