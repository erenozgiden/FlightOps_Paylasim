// Kullanıcı panelindeki Türkiye uçuş haritası
// Sınırlar harita-veri.js'den gelir; uçuş ve havalimanı verisi sayfadaki #harita-json'dan okunur.
(function () {
  const svg = document.getElementById("harita");
  const veriEl = document.getElementById("harita-json");
  if (!svg || !veriEl || !window.HARITA_VERISI) return;

  const veri = JSON.parse(veriEl.textContent);
  const NS = "http://www.w3.org/2000/svg";

  // harita-veri.js ile aynı izdüşüm
  const LON0 = 25.0, LAT1 = 42.8, K = 40, C = Math.cos(39 * Math.PI / 180);
  const W = 637, H = 296, KENAR = 14;
  const izdus = (enlem, boylam) => [(boylam - LON0) * C * K, (LAT1 - enlem) * K];
  const haritaIcinde = ([x, y]) => x >= KENAR && x <= W - KENAR && y >= KENAR && y <= H - KENAR;

  const RENK = {
    "etiket-mavi": "var(--mavi)", "etiket-yesil": "var(--yesil)", "etiket-sari": "var(--sari)",
    "etiket-kirmizi": "var(--kirmizi)", "etiket-gri": "var(--soluk)"
  };

  function ekle(ust, ad, ozellikler, metin) {
    const el = document.createElementNS(NS, ad);
    for (const [k, v] of Object.entries(ozellikler || {})) el.setAttribute(k, v);
    if (metin != null) el.textContent = metin;
    ust.appendChild(el);
    return el;
  }

  svg.setAttribute("viewBox", `0 0 ${W} ${H}`);
  ekle(svg, "rect", { x: 0, y: 0, width: W, height: H, class: "harita-deniz" });
  ekle(svg, "path", { d: HARITA_VERISI.komsu, class: "harita-komsu" });
  ekle(svg, "path", { d: HARITA_VERISI.turkiye, class: "harita-turkiye" });

  const katmanRota = ekle(svg, "g", {});
  const katmanNokta = ekle(svg, "g", {});
  const katmanYazi = ekle(svg, "g", {});

  const havalimani = Object.fromEntries(veri.havalimanlari.map(h => [h.kod, { ...h, nokta: izdus(h.enlem, h.boylam) }]));
  const kullanilanKodlar = new Set(veri.ucuslar.flatMap(u => [u.kalkisKod, u.varisKod]).filter(Boolean));
  const seciliKalkis = veri.seciliKalkis || "";
  const seciliVaris = veri.seciliVaris || "";

  // Harita dışındaki (yurt dışı) bir noktaya giden çizgiyi harita kenarında keser
  function kenaraKes(a, b) {
    let t = 1;
    const [dx, dy] = [b[0] - a[0], b[1] - a[1]];
    if (dx < 0) t = Math.min(t, (KENAR - a[0]) / dx);
    if (dx > 0) t = Math.min(t, (W - KENAR - a[0]) / dx);
    if (dy < 0) t = Math.min(t, (KENAR - a[1]) / dy);
    if (dy > 0) t = Math.min(t, (H - KENAR - a[1]) / dy);
    return [a[0] + dx * t, a[1] + dy * t];
  }

  // ---- İpucu kutusu ----
  const ipucu = document.getElementById("harita-ipucu");
  function ipucuGoster(e, html) {
    ipucu.innerHTML = html;
    ipucu.hidden = false;
    const kutu = svg.parentElement.getBoundingClientRect();
    ipucu.style.left = Math.min(e.clientX - kutu.left + 12, kutu.width - ipucu.offsetWidth - 4) + "px";
    ipucu.style.top = (e.clientY - kutu.top + 12) + "px";
  }
  const ipucuGizle = () => (ipucu.hidden = true);

  // ---- Rotalar: önce eşleşmeyenler (soluk), sonra eşleşenler (renkli, üstte) ----
  const yurtDisiEtiketleri = new Map();
  const ucuslar = [...veri.ucuslar].sort((a, b) => a.eslesti - b.eslesti);

  // Aynı iki şehir arasındaki uçuşlar üst üste binmesin diye kavisleri farklılaştır
  const ciftSayaci = {};

  for (const u of ucuslar) {
    const a = havalimani[u.kalkisKod], b = havalimani[u.varisKod];
    if (!a || !b) continue;

    let p1 = a.nokta, p2 = b.nokta, disari = null;
    if (!haritaIcinde(p2)) { disari = b; p2 = kenaraKes(p1, p2); }
    if (!haritaIcinde(p1)) { disari = a; p1 = kenaraKes(p2, p1); }

    const anahtar = [u.kalkisKod, u.varisKod].sort().join("-");
    const sira = (ciftSayaci[anahtar] = (ciftSayaci[anahtar] || 0) + 1);
    const [mx, my] = [(p1[0] + p2[0]) / 2, (p1[1] + p2[1]) / 2];
    const [dx, dy] = [p2[0] - p1[0], p2[1] - p1[1]];
    const uzunluk = Math.hypot(dx, dy) || 1;
    const egim = (disari ? 0.08 : 0.18) * sira;
    const [kx, ky] = [mx - dy * egim, my + dx * egim];
    const d = `M${p1[0]},${p1[1]} Q${kx},${ky} ${p2[0]},${p2[1]}`;

    const renk = RENK[u.durumSinifi] || "var(--soluk)";
    const g = ekle(katmanRota, "g", { class: "rota " + (u.eslesti ? "eslesti" : "soluk-rota"), tabindex: u.eslesti ? 0 : -1 });
    ekle(g, "path", { d, class: "rota-alan" });                 // tıklama/üzerine gelme alanı
    const cizgi = ekle(g, "path", { d, class: "rota-cizgi" });
    if (u.eslesti) {
      cizgi.style.stroke = renk;
      // Rota ortasına uçuş yönünde küçük uçak
      const [ux, uy] = [0.25 * p1[0] + 0.5 * kx + 0.25 * p2[0], 0.25 * p1[1] + 0.5 * ky + 0.25 * p2[1]];
      const aci = Math.atan2(p2[1] - ky, p2[0] - kx) * 180 / Math.PI;
      ekle(g, "text", { x: 0, y: 0, class: "rota-ucak", transform: `translate(${ux},${uy}) rotate(${aci + 45})`, fill: renk }, "✈");
    }

    if (disari) {
      const kenar = disari === b ? p2 : p1;
      if (!yurtDisiEtiketleri.has(disari.kod)) yurtDisiEtiketleri.set(disari.kod, { nokta: kenar, havalimani: disari });
    }

    const icerik = `<strong>${u.ucusNo}</strong> ${u.kalkisSehir} → ${u.varisSehir}<br>` +
      `${u.zaman} · ${u.durum} · ${u.fiyat}` + (u.eslesti ? "" : "<br><em>Filtreye uymuyor</em>");
    g.addEventListener("mousemove", (e) => ipucuGoster(e, icerik));
    g.addEventListener("mouseleave", ipucuGizle);
    g.addEventListener("click", () => {
      const kart = document.getElementById("ucus-" + u.id);
      if (!kart) return;
      kart.scrollIntoView({ behavior: "smooth", block: "center" });
      kart.classList.remove("vurgu");
      void kart.offsetWidth;
      kart.classList.add("vurgu");
    });
  }

  // ---- Yurt dışı uçuşların kenardaki etiketleri ----
  for (const { nokta, havalimani: h } of yurtDisiEtiketleri.values()) {
    const sag = nokta[0] > W / 2, alt = nokta[1] > H / 2;
    ekle(katmanNokta, "circle", { cx: nokta[0], cy: nokta[1], r: 3, class: "yurtdisi-nokta" });
    ekle(katmanYazi, "text", {
      x: nokta[0] + (sag ? -6 : 6), y: nokta[1] + (alt ? -6 : 12),
      "text-anchor": sag ? "end" : "start", class: "sehir-yazi yurtdisi"
    }, `${h.sehir} (${h.kod}) ↗`);
  }

  // ---- Havalimanları ----
  const yazilanSehirler = new Set();
  for (const h of Object.values(havalimani)) {
    if (!haritaIcinde(h.nokta)) continue;
    if (h.yurtDisi && !kullanilanKodlar.has(h.kod)) continue;

    const kullaniliyor = kullanilanKodlar.has(h.kod);
    const secili = h.sehir === seciliKalkis ? "secili-kalkis" : h.sehir === seciliVaris ? "secili-varis" : "";
    const g = ekle(katmanNokta, "g", { class: `havalimani ${kullaniliyor ? "aktif" : ""} ${secili}`, tabindex: kullaniliyor ? 0 : -1 });
    ekle(g, "circle", { cx: h.nokta[0], cy: h.nokta[1], r: kullaniliyor ? 4.5 : 2.5 });
    ekle(g, "circle", { cx: h.nokta[0], cy: h.nokta[1], r: 12, class: "tiklama-alani" });  // kolay tıklama
    if (secili) ekle(g, "circle", { cx: h.nokta[0], cy: h.nokta[1], r: 9, class: "secim-halkasi" });

    if (kullaniliyor && !yazilanSehirler.has(h.sehir)) {
      yazilanSehirler.add(h.sehir);
      ekle(katmanYazi, "text", { x: h.nokta[0] + 7, y: h.nokta[1] - 6, class: "sehir-yazi " + secili }, h.sehir);
    }

    g.addEventListener("mousemove", (e) => ipucuGoster(e, `<strong>${h.sehir}</strong> · ${h.ad} (${h.kod})` +
      (kullaniliyor ? "<br>Seçmek için tıklayın" : "<br>Bu şehirde uçuş yok")));
    g.addEventListener("mouseleave", ipucuGizle);
    if (kullaniliyor) g.addEventListener("click", () => sehirSec(h.sehir));
  }

  // ---- Şehre tıklayınca Nereden / Nereye seç ----
  const form = document.getElementById("ucus-filtre");
  const nereden = document.getElementById("Kalkis");
  const nereye = document.getElementById("Varis");

  function sehirSec(sehir) {
    if (!nereden.value || (nereden.value && nereye.value)) {
      nereden.value = sehir;
      nereye.value = "";
    } else if (sehir === nereden.value) {
      nereden.value = "";
    } else {
      nereye.value = sehir;
    }
    form.submit();
  }

  // Nereden ⇄ Nereye
  document.getElementById("yer-degistir")?.addEventListener("click", () => {
    [nereden.value, nereye.value] = [nereye.value, nereden.value];
    form.submit();
  });
  [nereden, nereye].forEach(s => s.addEventListener("change", () => form.submit()));
})();
