// Logo bulunamazsa eskisi gibi gösterilmez
document.querySelectorAll("img.logo").forEach((img) => {
  img.addEventListener("error", () => img.remove());
});

// Üst bilgideki saat (eski uygulamadaki gibi saniye saniye)
const saat = document.getElementById("saat");
if (saat) {
  const guncelle = () => (saat.textContent = new Date().toLocaleTimeString("tr-TR", { hour12: false }));
  guncelle();
  setInterval(guncelle, 1000);
}

// data-onay="..." olan formlar göndermeden önce onay ister (silme, iptal vb.)
document.querySelectorAll("form[data-onay]").forEach((form) => {
  form.addEventListener("submit", (e) => {
    if (!confirm(form.dataset.onay)) e.preventDefault();
  });
});

// data-onizleme="resimId" olan dosya seçicilerde seçilen fotoğrafı hemen göster
document.querySelectorAll("input[type=file][data-onizleme]").forEach((girdi) => {
  girdi.addEventListener("change", () => {
    const dosya = girdi.files[0];
    if (!dosya) return;
    const img = document.getElementById(girdi.dataset.onizleme);
    img.src = URL.createObjectURL(dosya);
    img.hidden = false;
    document.getElementById("foto-yok")?.remove();
  });
});

// Bildirimler birkaç saniye sonra kaybolur
document.querySelectorAll(".bildirim").forEach((b) => {
  setTimeout(() => b.classList.add("gizle"), 4000);
});
