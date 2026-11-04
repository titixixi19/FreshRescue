document.addEventListener("DOMContentLoaded", function () {
  const tableEl = document.getElementById("offers-manage-table");
  if (!tableEl) return;

  const statusBadge = document.getElementById("signalr-status-badge");

  function updateConnectionStatus(text, className) {
    if (!statusBadge) return;
    statusBadge.textContent = text;
    statusBadge.className = "badge " + className;
  }

  /**
   * Ghép Api:BaseUrl (từ data-api-base do Razor inject) + /hubs/offers.
   * Không hardcode host/port. Bỏ slash thừa ở cuối BaseUrl.
   */
  function resolveHubUrl() {
    var raw =
      (tableEl.getAttribute("data-api-base") || "").trim() ||
      (window.freshRescue && window.freshRescue.apiBaseUrl
        ? String(window.freshRescue.apiBaseUrl).trim()
        : "");

    if (!raw) {
      console.error("Thiếu Api:BaseUrl — không thể kết nối SignalR hub.");
      return null;
    }

    var base = raw.replace(/\/+$/, "");
    return base + "/hubs/offers";
  }

  var hubUrl = resolveHubUrl();
  if (!hubUrl) {
    updateConnectionStatus("Thiếu cấu hình API", "badge-err");
    return;
  }

  console.log("SignalR hub URL:", hubUrl);

  if (typeof signalR === "undefined") {
    console.error("Thư viện SignalR chưa được tải.");
    updateConnectionStatus("Thiếu thư viện", "badge-err");
    return;
  }

  var connection = new signalR.HubConnectionBuilder()
    .withUrl(hubUrl)
    .withAutomaticReconnect()
    .build();

  // Lấy saleBatchId từ thuộc tính data của bảng (được Razor truyền vào)
  var saleBatchId = tableEl.getAttribute("data-sale-batch-id");

  // Hàm phụ trợ để gọi JoinBatch an toàn khi đã kết nối
  function joinBatch() {
    if (saleBatchId && connection.state === signalR.HubConnectionState.Connected) {
      connection.invoke("JoinBatch", saleBatchId)
        .catch(function (err) {
          console.error("Lỗi khi join batch:", err);
        });
    }
  }

  // Contract: OfferQuantityChanged — payload offerId, remainingQuantity, saleBatchId
  connection.on("OfferQuantityChanged", function (data) {
    if (!data) return;

    var offerId = data.offerId || data.OfferId;
    var remaining = data.remainingQuantity ?? data.RemainingQuantity;
    if (!offerId) return;

    var row = document.querySelector('tr[data-offer-id="' + offerId + '"]');
    if (!row) return;

    var remainingCell = row.querySelector(".remaining-qty-text");
    if (remainingCell) {
      remainingCell.textContent = remaining;
      row.style.transition = "background-color 0.5s";
      row.style.backgroundColor = "#e0f2fe";
      setTimeout(function () {
        row.style.backgroundColor = "";
      }, 1000);
    }
  });

  connection.onreconnecting(function () {
    updateConnectionStatus("Đang kết nối lại...", "badge-warn");
  });

  // Yêu cầu: invoke("JoinBatchAsync", saleBatchId) sau start() và trong onreconnected
  connection.onreconnected(function () {
    updateConnectionStatus("Đã kết nối", "badge-ok");
    joinBatch();
  });

  connection.onclose(function () {
    updateConnectionStatus("Mất kết nối", "badge-err");
  });

  updateConnectionStatus("Đang kết nối...", "badge-warn");
  connection
    .start()
    .then(function () {
      updateConnectionStatus("Đã kết nối", "badge-ok");
      joinBatch(); // Gọi ngay sau khi start() thành công
    })
    .catch(function (err) {
      console.error("SignalR connection error:", err && err.toString ? err.toString() : err);
      updateConnectionStatus("Lỗi kết nối", "badge-err");
    });
});
