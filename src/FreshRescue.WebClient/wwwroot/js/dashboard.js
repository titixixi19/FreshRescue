(function () {
  var toggle = document.getElementById("sidebarToggle");
  var sidebar = document.getElementById("dashSidebar");
  if (!toggle || !sidebar) return;

  toggle.addEventListener("click", function () {
    sidebar.classList.toggle("open");
  });

  document.addEventListener("click", function (e) {
    if (
      window.matchMedia("(max-width: 900px)").matches &&
      sidebar.classList.contains("open") &&
      !sidebar.contains(e.target) &&
      e.target !== toggle &&
      !toggle.contains(e.target)
    ) {
      sidebar.classList.remove("open");
    }
  });
})();
