document.addEventListener("DOMContentLoaded", function () {
  // Kiểm tra xem trang có chứa bảng quản lý offer hay không trước khi khởi tạo SignalR
  const tableEl = document.getElementById("offers-manage-table");
  if (!tableEl) return;

  const statusBadge = document.getElementById("signalr-status-badge");

  function updateConnectionStatus(text, className) {
    if (!statusBadge) return;
    statusBadge.textContent = text;
    statusBadge.className = "badge " + className;
  }

  // Khởi tạo kết nối SignalR Hub theo contract /hubs/offers
  const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/offers")
    .withAutomaticReconnect()
    .build();

  // Lắng nghe sự kiện thực tế từ backend: OfferQuantityChanged (payload: offerId, remainingQuantity, saleBatchId)
  connection.on("OfferQuantityChanged", function (data) {
    if (!data) return;
    const offerId = data.offerId || data.OfferId;
    const remaining = data.remainingQuantity ?? data.RemainingQuantity;

    if (!offerId) return;

    // Tìm dòng tương ứng trên bảng quản lý offer
    const row = document.querySelector(`tr[data-offer-id="${offerId}"]`);
    if (row) {
      const remainingCell = row.querySelector(".remaining-qty-text");
      if (remainingCell) {
        remainingCell.textContent = remaining;
        // Hiệu ứng nhấp nháy nhẹ báo hiệu cập nhật
        row.style.transition = "background-color 0.5s";
        row.style.backgroundColor = "#e0f2fe";
        setTimeout(() => {
          row.style.backgroundColor = "";
        }, 1000);
      }
    }
  });

  connection.onreconnecting(() => {
    updateConnectionStatus("Đang kết nối lại...", "badge-warn");
  });

  connection.onreconnected(() => {
    updateConnectionStatus("Đã kết nối", "badge-ok");
  });

  connection.onclose(() => {
    updateConnectionStatus("Mất kết nối", "badge-err");
  });

  // Bắt đầu kết nối Hub
  updateConnectionStatus("Đang kết nối...", "badge-warn");
  connection.start()
    .then(() => {
      updateConnectionStatus("Đã kết nối", "badge-ok");
      console.log("SignalR Connected to /hubs/offers.");
    })
    .catch(err => {
      console.error("SignalR Connection Error: ", err);
      updateConnectionStatus("Lỗi kết nối", "badge-err");
    });
});
