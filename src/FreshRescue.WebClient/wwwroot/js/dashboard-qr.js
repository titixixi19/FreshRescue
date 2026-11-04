document.addEventListener("DOMContentLoaded", function () {
  const startBtn = document.getElementById("start-camera-btn");
  const stopBtn = document.getElementById("stop-camera-btn");
  const statusSpan = document.getElementById("camera-status");
  const readerEl = document.getElementById("reader");
  const banner = document.getElementById("result-banner");
  const tokenHolder = document.getElementById("ajax-redeem-token-holder");

  if (!readerEl || !startBtn) return;

  let html5QrCode = null;
  let isScanning = false;
  let redeemInFlight = false;

  function getAntiForgeryToken() {
    if (!tokenHolder) return "";
    const input = tokenHolder.querySelector('input[name="__RequestVerificationToken"]');
    return input ? input.value : "";
  }

  function showBanner(ok, message) {
    if (!banner) return;
    banner.style.display = "block";
    banner.className = ok ? "dash-alert dash-alert-ok" : "dash-alert dash-alert-err";
    banner.innerHTML = (ok ? "<strong>Thành công.</strong> " : "<strong>Lỗi.</strong> ") +
      (message || "");
  }

  async function stopCamera() {
    if (!html5QrCode) return;
    try {
      if (isScanning) {
        await html5QrCode.stop();
      }
      html5QrCode.clear();
    } catch (e) {
      console.error(e);
    }
    isScanning = false;
    stopBtn.style.display = "none";
    startBtn.style.display = "inline-block";
    startBtn.disabled = false;
  }

  async function redeemToken(decodedText) {
    if (redeemInFlight) return;
    redeemInFlight = true;
    statusSpan.textContent = "Đã đọc QR — đang gửi token lên server...";

    const body = new URLSearchParams();
    body.append("code", decodedText);
    body.append("__RequestVerificationToken", getAntiForgeryToken());

    try {
      const res = await fetch("/Dashboard/Redemptions/RedeemAjax", {
        method: "POST",
        headers: {
          "Content-Type": "application/x-www-form-urlencoded",
          "RequestVerificationToken": getAntiForgeryToken()
        },
        body: body.toString(),
        credentials: "same-origin"
      });

      const data = await res.json();
      if (data.success) {
        showBanner(true, data.message || ("OK: " + (data.orderCode || "")));
        statusSpan.textContent = "Đối soát thành công. Camera đã tắt.";
      } else {
        showBanner(false, data.message || "Redeem thất bại.");
        statusSpan.textContent = "Redeem thất bại. Có thể bật lại camera.";
      }
    } catch (err) {
      console.error(err);
      showBanner(false, "Không gọi được server (mạng / session).");
      statusSpan.textContent = "Lỗi mạng.";
    } finally {
      redeemInFlight = false;
    }
  }

  startBtn.addEventListener("click", async function () {
    if (isScanning || redeemInFlight) return;

    startBtn.disabled = true;
    statusSpan.textContent = "Đang khởi động camera...";

    try {
      html5QrCode = new Html5Qrcode("reader");
      const qrConfig = { fps: 10, qrbox: { width: 250, height: 250 } };

      await html5QrCode.start(
        { facingMode: "environment" },
        qrConfig,
        async function onScanSuccess(decodedText) {
          if (!isScanning || redeemInFlight) return;
          await stopCamera();
          statusSpan.textContent = "Đã quét: " + decodedText;
          await redeemToken(decodedText);
        },
        function onScanFailure() { /* frame miss — bỏ qua */ }
      );

      isScanning = true;
      startBtn.style.display = "none";
      stopBtn.style.display = "inline-block";
      statusSpan.textContent = "Đang quét... Hướng camera vào mã QR.";
    } catch (err) {
      console.error("Không thể mở camera:", err);
      statusSpan.textContent =
        "Không thể truy cập camera (thiếu quyền hoặc không có thiết bị).";
      startBtn.disabled = false;
    }
  });

  stopBtn.addEventListener("click", async function () {
    await stopCamera();
    statusSpan.textContent = "Camera đã tắt.";
  });

  window.addEventListener("beforeunload", function () {
    if (html5QrCode && isScanning) {
      try { html5QrCode.stop(); } catch (e) { /* ignore */ }
    }
  });
});
