// Quick Checkout (T4-TV4-02, 01-REQUIREMENTS.md §9.3): chọn món → đặt giữ chỗ → thanh toán giả lập → hiện QR.
// Gọi controller của WebClient bằng fetch (Ch.07); controller chuyển tiếp sang API kèm token trong cookie.
// Mở modal bằng nút có data-qc (Areas/Customer/Models/QuickCheckout.cs); trang Checkout/Pay dùng lại renderPayment.
(function () {
    'use strict';

    const modalElement = document.getElementById('qc-modal');
    if (!modalElement) {
        return;
    }

    // 03-API-CONTRACT.md §4.1: 1–20 dòng, mỗi dòng 1–10 suất — chặn trước khi gửi.
    const MAX_LINES = 20;
    const MAX_QUANTITY = 10;
    const STEPS = ['items', 'pay', 'ticket'];

    const urls = modalElement.dataset;
    const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
    const modalBody = document.getElementById('qc-body');
    const esc = FR.escapeHtml;
    const money = FR.formatMoney;
    let orderCreated = false;

    function urlFor(template, id) {
        return template.replace('__id__', encodeURIComponent(id));
    }

    function setStep(step) {
        const current = STEPS.indexOf(step);
        modalElement.querySelectorAll('.fr-steps [data-step]').forEach((item) => {
            const index = STEPS.indexOf(item.dataset.step);
            item.classList.toggle('is-active', index === current);
            item.classList.toggle('is-done', index < current);
        });
    }

    function alertHtml(kind, message, extraHtml = '') {
        return `<div class="alert alert-${kind} small py-2" role="alert">${esc(message)}${extraHtml}</div>`;
    }

    function errorHtml(error) {
        // Bắt theo errorCode, không theo câu chữ (03 §1.3).
        const reload = error.errorCode === 'OFFER_SOLD_OUT'
            ? ' <button type="button" class="btn btn-link btn-sm p-0 align-baseline" onclick="location.reload()">Tải lại danh sách</button>'
            : '';
        return alertHtml('danger', error.message, reload);
    }

    function setBusy(button, busy) {
        if (busy) {
            button.dataset.label = button.innerHTML;
            button.innerHTML = '<span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>Đang xử lý…';
        } else if (button.dataset.label) {
            button.innerHTML = button.dataset.label;
        }
        button.disabled = busy;
    }

    // ---------- Bước 1: chọn món ----------

    function renderItems(data) {
        setStep('items');

        const lines = data.offers.map((offer) => {
            const limit = Math.min(offer.remainingQuantity, offer.maxPerCustomer, MAX_QUANTITY);
            return `
                <div class="fr-qc-line" data-offer-id="${esc(offer.id)}" data-price="${offer.actualPrice}">
                    <div class="flex-grow-1 me-2">
                        <div class="fw-semibold">${esc(offer.name)}</div>
                        <div class="small text-muted">
                            ${money(offer.actualPrice)} <s>${money(offer.originalPrice)}</s> ·
                            ${limit > 0 ? `còn ${offer.remainingQuantity} suất` : '<span class="text-danger">hết suất</span>'}
                        </div>
                    </div>
                    <div class="input-group input-group-sm fr-qty">
                        <button type="button" class="btn btn-outline-secondary" data-delta="-1" aria-label="Bớt một" ${limit === 0 ? 'disabled' : ''}>−</button>
                        <input type="number" class="form-control text-center" min="0" max="${limit}" value="${Math.min(offer.quantity, limit)}"
                               aria-label="Số lượng ${esc(offer.name)}" ${limit === 0 ? 'disabled' : ''} />
                        <button type="button" class="btn btn-outline-secondary" data-delta="1" aria-label="Thêm một" ${limit === 0 ? 'disabled' : ''}>+</button>
                    </div>
                </div>`;
        }).join('');

        modalBody.innerHTML = `
            <p class="small text-muted mb-2">🏪 ${esc(data.storeName)} · Nhận ${FR.formatWindow(data.pickupStartTime, data.pickupEndTime)}</p>
            <div class="fr-qc-lines">${lines}</div>
            <div class="d-flex justify-content-between align-items-center border-top pt-3 mt-2">
                <span>Tạm tính</span><strong class="fs-5" id="qc-subtotal"></strong>
            </div>
            <p class="small text-muted mt-2">Đơn được giữ chỗ 10 phút để thanh toán. Mỗi người mua tối đa ${data.offers[0]?.maxPerCustomer ?? 3} suất cho mỗi món trong một đợt.</p>
            <div id="qc-alert"></div>
            <button type="button" class="btn btn-success w-100" id="qc-submit">Đặt &amp; giữ chỗ</button>`;

        const inputs = [...modalBody.querySelectorAll('.fr-qc-line input')];
        const subtotal = modalBody.querySelector('#qc-subtotal');
        const submit = modalBody.querySelector('#qc-submit');

        const clamp = (input) => {
            const max = Number(input.max);
            const value = Math.round(Number(input.value) || 0);
            input.value = Math.max(0, Math.min(max, value));
        };

        const refresh = () => {
            let total = 0;
            let count = 0;
            inputs.forEach((input) => {
                const quantity = Number(input.value) || 0;
                total += quantity * Number(input.closest('.fr-qc-line').dataset.price);
                count += quantity;
            });
            subtotal.textContent = money(total);
            submit.disabled = count === 0;
        };

        modalBody.querySelectorAll('[data-delta]').forEach((button) => {
            button.addEventListener('click', () => {
                const input = button.parentElement.querySelector('input');
                input.value = (Number(input.value) || 0) + Number(button.dataset.delta);
                clamp(input);
                refresh();
            });
        });
        inputs.forEach((input) => input.addEventListener('input', () => { clamp(input); refresh(); }));
        refresh();

        submit.addEventListener('click', async () => {
            const items = inputs
                .map((input) => ({ offerId: input.closest('.fr-qc-line').dataset.offerId, quantity: Number(input.value) || 0 }))
                .filter((item) => item.quantity > 0);
            const alertBox = modalBody.querySelector('#qc-alert');

            if (items.length === 0 || items.length > MAX_LINES) {
                alertBox.innerHTML = alertHtml('warning', 'Chọn ít nhất một món.');
                return;
            }

            setBusy(submit, true);
            const result = await FR.api(urls.createUrl, { method: 'POST', body: { saleBatchId: data.saleBatchId, items } });
            if (!result.ok) {
                alertBox.innerHTML = errorHtml(result.data);
                setBusy(submit, false);
                return;
            }

            orderCreated = true;
            renderPayment(modalBody, result.data);
        });
    }

    // ---------- Bước 2: thanh toán giả lập (OQ-17) ----------

    function renderPayment(container, order) {
        setStep('pay');

        const items = order.items.map((item) => `
            <li class="list-group-item d-flex justify-content-between px-0">
                <span>${esc(item.offerName)} × ${item.quantity}</span><span>${money(item.lineTotal)}</span>
            </li>`).join('');

        container.innerHTML = `
            <div class="text-center mb-3">
                <div class="small text-muted">Đã giữ chỗ — mã đơn</div>
                <div class="fr-order-code">${esc(order.orderCode)}</div>
            </div>
            <ul class="list-group list-group-flush small mb-3">
                ${items}
                <li class="list-group-item d-flex justify-content-between px-0 fw-bold">
                    <span>Tổng cộng</span><span>${money(order.totalAmount)}</span>
                </li>
            </ul>
            <div class="alert alert-warning small py-2">
                ⏳ Thanh toán trước <strong>${FR.formatTime(order.paymentDeadline)}</strong>
                (còn <strong data-countdown="${esc(order.paymentDeadline)}"></strong>) — quá hạn đơn tự huỷ và trả suất.
            </div>
            <p class="small text-muted">
                🏪 ${esc(order.storeName)} — ${esc(order.storeAddress)}<br />
                Nhận hàng ${FR.formatWindow(order.pickupStartTime, order.pickupEndTime)}
            </p>
            <div class="fr-fake-gateway">
                <div class="small text-muted mb-2">💳 Cổng thanh toán giả lập (demo)</div>
                <div data-pay-alert></div>
                <div class="d-grid gap-2">
                    <button type="button" class="btn btn-success" data-outcome="Success">Thanh toán ${money(order.totalAmount)}</button>
                    <button type="button" class="btn btn-outline-danger btn-sm" data-outcome="Failure">Giả lập thanh toán thất bại</button>
                </div>
            </div>`;

        const buttons = [...container.querySelectorAll('[data-outcome]')];
        const alertBox = container.querySelector('[data-pay-alert]');
        const orderLink = ` <a href="${urlFor(urls.orderUrl, order.id)}">Xem đơn</a>`;

        FR.startCountdowns(container);
        container.querySelector('[data-countdown]').addEventListener('fr:expired', () => {
            buttons.forEach((button) => { button.disabled = true; });
            alertBox.innerHTML = alertHtml('danger', 'Đã quá hạn thanh toán, đơn sẽ được huỷ tự động.', orderLink);
        });

        buttons.forEach((button) => button.addEventListener('click', async () => {
            buttons.forEach((other) => { other.disabled = true; });
            setBusy(button, true);
            alertBox.innerHTML = '';

            const result = await FR.api(urlFor(urls.payUrl, order.id), { method: 'POST', body: { outcome: button.dataset.outcome } });
            setBusy(button, false);

            if (!result.ok) {
                const final = ['PAYMENT_DEADLINE_PASSED', 'ORDER_NOT_PAYABLE'].includes(result.data.errorCode);
                alertBox.innerHTML = alertHtml('danger', result.data.message, final ? orderLink : '');
                buttons.forEach((other) => { other.disabled = final; });
                return;
            }

            if (result.data.qrTicket) {
                renderTicket(container, result.data.order, result.data.qrTicket);
                return;
            }

            // Thất bại không huỷ đơn — tồn kho vẫn giữ tới hạn thanh toán (OQ-17).
            alertBox.innerHTML = alertHtml('warning', `Thanh toán không thành công. Đơn vẫn được giữ tới ${FR.formatTime(order.paymentDeadline)} — bạn có thể thử lại.`);
            buttons.forEach((other) => { other.disabled = false; });
        }));
    }

    // ---------- Bước 3: vé QR (FR-C07) ----------

    function renderTicket(container, order, ticket) {
        setStep('ticket');

        container.innerHTML = `
            <div class="text-center">
                <div class="fr-success-mark" aria-hidden="true">✓</div>
                <h3 class="h5 mb-1">Thanh toán thành công!</h3>
                <p class="small text-muted">Đến quầy trong khung giờ nhận và đưa mã này cho nhân viên quét.</p>
                <div class="fr-qr mx-auto" data-qr></div>
                <div class="small text-muted mt-2">Mã đơn — đọc cho nhân viên nếu không quét được</div>
                <div class="fr-order-code">${esc(order.orderCode)}</div>
                <p class="small my-3">
                    🏪 <strong>${esc(order.storeName)}</strong><br />${esc(order.storeAddress)}<br />
                    Nhận hàng <strong>${FR.formatWindow(order.pickupStartTime, order.pickupEndTime)}</strong>
                </p>
                <div class="d-grid gap-2">
                    <a class="btn btn-success" href="${urlFor(urls.ticketUrl, order.id)}">Mở vé toàn màn hình</a>
                    <a class="btn btn-outline-secondary" href="${urls.ordersUrl}">Xem đơn của tôi</a>
                </div>
            </div>`;

        FR.renderQr(container.querySelector('[data-qr]'), ticket.token);
    }

    // ---------- Gắn sự kiện ----------

    document.addEventListener('click', (event) => {
        const trigger = event.target.closest('[data-qc]');
        if (!trigger) {
            return;
        }
        if (!FR.isAuthenticated) {
            FR.goToLogin();
            return;
        }

        orderCreated = false;
        if (FR.isCustomer) {
            renderItems(JSON.parse(trigger.dataset.qc));
        } else {
            setStep('items');
            modalBody.innerHTML = alertHtml('warning', 'Chức năng mua hàng chỉ dành cho tài khoản khách hàng. Hãy đăng nhập bằng tài khoản khách để đặt món.');
        }
        modal.show();
    });

    // Đã đặt đơn thì tải lại để số suất còn lại trên trang là số mới.
    modalElement.addEventListener('hidden.bs.modal', () => {
        if (orderCreated) {
            location.reload();
        }
    });

    // Trang Checkout/Pay: thanh toán đơn đã đặt từ trước.
    const payPage = document.getElementById('fr-pay');
    if (payPage) {
        renderPayment(payPage, JSON.parse(payPage.dataset.order));
    }
})();
