/* FURKAN AI — istemci yardımcıları: tema, kaydırma, kod kopyalama, metin alanı boyutu */
(function () {
  "use strict";
  let net = null;

  window.fa = {
    kur(dotnet) {
      net = dotnet;
      // Kod bloklarındaki "Kopyala" düğmeleri (Markdown HTML'i C#'ta üretilir; olay burada yakalanır)
      document.addEventListener("click", (e) => {
        const b = e.target.closest(".kod-kopyala");
        if (!b) return;
        const kod = b.closest(".kod")?.querySelector("code")?.innerText ?? "";
        if (net) net.invokeMethodAsync("Kopyala", kod).catch(() => { });
        b.textContent = "Kopyalandı ✓";
        setTimeout(() => (b.textContent = "Kopyala"), 1500);
      });
    },
    tema(ad, vurgu, azAnimasyon, yazi) {
      document.body.setAttribute("data-theme", ad);
      document.body.setAttribute("data-vurgu", vurgu);
      document.body.setAttribute("data-yazi", yazi || "normal");
      document.body.classList.toggle("az-animasyon", !!azAnimasyon);
      try { localStorage.setItem("fa-tema", ad); localStorage.setItem("fa-vurgu", vurgu); } catch (e) { }
    },
    geri() { history.back(); },
    yukari() { window.scrollTo({ top: 0, behavior: "smooth" }); },
    odakla(id) { const el = document.getElementById(id); if (el) el.focus(); },
    /** Sayfa sonuna yakınsa (veya zorla) en alta kaydırır — kullanıcı yukarıda okurken akış onu aşağı çekmez. */
    altaKaydir(zorla) {
      const kalan = document.documentElement.scrollHeight - window.innerHeight - window.scrollY;
      if (zorla || kalan < 260) window.scrollTo({ top: document.documentElement.scrollHeight, behavior: zorla ? "auto" : "smooth" });
    },
    altaYakinMi() { return document.documentElement.scrollHeight - window.innerHeight - window.scrollY < 260; },
    /** Metin alanını içeriğe göre büyütür (en fazla 6 satır kadar). */
    boyutla(id) {
      const t = document.getElementById(id);
      if (!t) return;
      t.style.height = "auto";
      t.style.height = Math.min(t.scrollHeight, 168) + "px";
    },
    temizle(id) { const t = document.getElementById(id); if (t) { t.value = ""; t.style.height = "auto"; } }
  };
})();
