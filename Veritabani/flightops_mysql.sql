-- =====================================================================
--  FlightOps - Uçuş Yönetim Sistemi
--  MySQL veritabanı dışa aktarma dosyası (tablo yapısı + kayıtlar)
--
--  Oluşturulma : 27.11.2026 19:55
--  Veriler     : Örnek veriler (uygulamanın ilk açılışta eklediği kayıtlar)
--                (5 kullanıcı, 5 uçuş, 5 bilet)
--  Uyumluluk   : MySQL 8.0.16 ve üzeri (MySQL 8.4 ile denendi)
--  Karakter    : utf8mb4, Türkçe sıralama (utf8mb4_turkish_ci)
--
--  İçe aktarma (dosya "flightops" veritabanını kendisi oluşturur):
--    Komut satırı : mysql -u root -p --default-character-set=utf8mb4 < flightops_mysql.sql
--    phpMyAdmin   : İçe Aktar sekmesi -> bu dosyayı seçin -> Git
--    Workbench    : File -> Open SQL Script -> Çalıştır (şimşek simgesi)
--
--  Notlar:
--  - Şifreler düz metin değil, özet (hash) olarak saklanır.
--  - Kredi kartlarının yalnızca son 4 hanesi saklanır; tam numara ve CVV yoktur.
--  - Profil fotoğrafları veritabanında değil dosya olarak tutulur; tabloda yalnızca yolları vardır.
-- =====================================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

CREATE DATABASE IF NOT EXISTS `flightops`
  DEFAULT CHARACTER SET utf8mb4
  DEFAULT COLLATE utf8mb4_turkish_ci;

USE `flightops`;

DROP TABLE IF EXISTS `Biletler`;
DROP TABLE IF EXISTS `Ucuslar`;
DROP TABLE IF EXISTS `Kullanicilar`;

-- ---------------------------------------------------------------------
--  Tablo yapısı
--  Kullanicilar : admin, personel ve kullanıcılar (Rol: 0 Admin, 1 Personel, 2 Kullanici)
--  Ucuslar      : uçuşlar
--  Biletler     : satılan biletler (Ucuslar ve Kullanicilar tablolarına bağlı)
-- ---------------------------------------------------------------------

ALTER DATABASE CHARACTER SET utf8mb4;


CREATE TABLE `Kullanicilar` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `KullaniciAdi` varchar(40) CHARACTER SET utf8mb4 COLLATE utf8mb4_turkish_ci NOT NULL,
    `SifreHash` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_turkish_ci NOT NULL COMMENT 'Şifre özeti (hash), düz metin değil',
    `AdSoyad` varchar(80) CHARACTER SET utf8mb4 COLLATE utf8mb4_turkish_ci NOT NULL,
    `Eposta` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_turkish_ci NULL COMMENT 'Şifremi unuttum doğrulaması için',
    `Telefon` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_turkish_ci NULL,
    `Rol` int NOT NULL COMMENT '0 = Admin, 1 = Personel, 2 = Kullanici',
    `Gorev` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_turkish_ci NULL COMMENT 'Personelin görevi (Pilot, Kabin Memuru ...)',
    `FotoYolu` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_turkish_ci NULL COMMENT 'Profil fotoğrafının dosya yolu',
    `Adres` varchar(300) CHARACTER SET utf8mb4 COLLATE utf8mb4_turkish_ci NULL COMMENT 'Ev adresi (kullanıcılar için zorunlu)',
    `KayitTarihi` datetime(6) NOT NULL,
    CONSTRAINT `PK_Kullanicilar` PRIMARY KEY (`Id`),
    CONSTRAINT `CK_Kullanicilar_Rol` CHECK (`Rol` BETWEEN 0 AND 2)
) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_turkish_ci COMMENT='Admin, personel ve kullanıcılar (Rol sütunu ile ayrılır)';


CREATE TABLE `Ucuslar` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UcusNo` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_turkish_ci NOT NULL,
    `Kalkis` varchar(40) CHARACTER SET utf8mb4 COLLATE utf8mb4_turkish_ci NOT NULL COMMENT 'Örn. İstanbul (IST)',
    `Varis` varchar(40) CHARACTER SET utf8mb4 COLLATE utf8mb4_turkish_ci NOT NULL COMMENT 'Örn. Ankara (ESB)',
    `KalkisZamani` datetime(6) NOT NULL,
    `VarisZamani` datetime(6) NOT NULL,
    `Kapi` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_turkish_ci NULL,
    `Kapasite` int NOT NULL,
    `Fiyat` decimal(10,2) NOT NULL COMMENT 'Bilet fiyatı (TL)',
    `Durum` int NOT NULL COMMENT '0 = Planlandı, 1 = Biniş, 2 = Kalktı, 3 = İndi, 4 = Gecikmeli, 5 = İptal',
    CONSTRAINT `PK_Ucuslar` PRIMARY KEY (`Id`),
    CONSTRAINT `CK_Ucuslar_Durum` CHECK (`Durum` BETWEEN 0 AND 5),
    CONSTRAINT `CK_Ucuslar_Kapasite` CHECK (`Kapasite` BETWEEN 1 AND 600),
    CONSTRAINT `CK_Ucuslar_Zaman` CHECK (`VarisZamani` > `KalkisZamani`)
) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_turkish_ci COMMENT='Uçuşlar';


CREATE TABLE `Biletler` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UcusId` int NOT NULL,
    `KullaniciId` int NOT NULL,
    `KoltukNo` int NOT NULL,
    `Fiyat` decimal(10,2) NOT NULL COMMENT 'Satın alındığı andaki fiyat (TL)',
    `SatinAlmaTarihi` datetime(6) NOT NULL,
    `Durum` int NOT NULL COMMENT '0 = Aktif, 1 = İptal',
    `KartSahibi` varchar(60) CHARACTER SET utf8mb4 COLLATE utf8mb4_turkish_ci NULL,
    `KartTipi` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_turkish_ci NULL COMMENT 'Visa, Mastercard, Troy, Amex',
    `KartSonDortHane` char(4) CHARACTER SET utf8mb4 COLLATE utf8mb4_turkish_ci NULL COMMENT 'Kartın yalnızca son 4 hanesi (tam numara ve CVV saklanmaz)',
    CONSTRAINT `PK_Biletler` PRIMARY KEY (`Id`),
    CONSTRAINT `CK_Biletler_Durum` CHECK (`Durum` IN (0, 1)),
    CONSTRAINT `FK_Biletler_Kullanicilar_KullaniciId` FOREIGN KEY (`KullaniciId`) REFERENCES `Kullanicilar` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_Biletler_Ucuslar_UcusId` FOREIGN KEY (`UcusId`) REFERENCES `Ucuslar` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_turkish_ci COMMENT='Satılan biletler (Ucuslar ve Kullanicilar tablolarına bağlı)';


CREATE INDEX `IX_Biletler_KullaniciId` ON `Biletler` (`KullaniciId`);


CREATE INDEX `IX_Biletler_UcusId` ON `Biletler` (`UcusId`);


CREATE UNIQUE INDEX `IX_Kullanicilar_KullaniciAdi` ON `Kullanicilar` (`KullaniciAdi`);


CREATE UNIQUE INDEX `IX_Ucuslar_UcusNo` ON `Ucuslar` (`UcusNo`);

-- ---------------------------------------------------------------------
--  Kayıtlar
-- ---------------------------------------------------------------------

-- Kullanicilar: 5 kayıt
INSERT INTO `Kullanicilar` (`Id`, `KullaniciAdi`, `SifreHash`, `AdSoyad`, `Eposta`, `Telefon`, `Rol`, `Gorev`, `FotoYolu`, `Adres`, `KayitTarihi`) VALUES
  (1, 'admin', 'AQAAAAIAAYagAAAAEHdsx529mS8fpE1LCnNjd6YYaWz9ReRAB71byLBIbsyVPX4AhFQuxPIr6R7lBsbknQ==', 'Sistem Yöneticisi', 'admin@flightops.local', NULL, 0, NULL, NULL, NULL, '2026-11-27 19:55:40.818720'),
  (2, 'personel', 'AQAAAAIAAYagAAAAEIs/66E+Vb8+eth7usAtR6IVBAzdpYA6MkGCu83Fps9FbXcrt0IvbmKS+hASTCWGPA==', 'Ayşe Yılmaz', NULL, '0532 000 00 01', 1, 'Operasyon Sorumlusu', NULL, NULL, '2026-11-27 19:55:40.858894'),
  (3, 'mkaya', 'AQAAAAIAAYagAAAAEE5O5nz7K7ew9q+Qu2Qf3BlvHFnTMTC9xm+w7LMG59AHB+aMRAx/1lFoXBIKqc42XA==', 'Mehmet Kaya', NULL, '0532 000 00 02', 1, 'Pilot', NULL, NULL, '2026-11-27 19:55:40.895771'),
  (4, 'kullanici', 'AQAAAAIAAYagAAAAELx/446M5wbw50HogV7936xg+7IPL1hfLK5b14a4SWHgo3gLoco2yWQWt1puJQjafQ==', 'Can Demir', 'can@ornek.com', NULL, 2, NULL, NULL, NULL, '2026-11-27 19:55:40.931994'),
  (5, 'zeynep', 'AQAAAAIAAYagAAAAEKlWcpK3ZayIKlLAEcvDnwY5ogf730BnHcv2bOhd8KbdZa+249IEN7+WJSvnu0/izQ==', 'Zeynep Arslan', 'zeynep@ornek.com', NULL, 2, NULL, NULL, NULL, '2026-11-27 19:55:40.968522');

-- Ucuslar: 5 kayıt
INSERT INTO `Ucuslar` (`Id`, `UcusNo`, `Kalkis`, `Varis`, `KalkisZamani`, `VarisZamani`, `Kapi`, `Kapasite`, `Fiyat`, `Durum`) VALUES
  (1, 'TK101', 'İstanbul (IST)', 'Ankara (ESB)', '2026-11-27 09:00:00.000000', '2026-11-27 10:10:00.000000', 'A3', 180, 1450.00, 2),
  (2, 'PC202', 'İstanbul (SAW)', 'İzmir (ADB)', '2026-11-27 13:00:00.000000', '2026-11-27 14:05:00.000000', 'B7', 189, 1200.00, 1),
  (3, 'TK305', 'Ankara (ESB)', 'Antalya (AYT)', '2026-11-28 08:00:00.000000', '2026-11-28 09:15:00.000000', 'C1', 160, 1650.00, 0),
  (4, 'AJ410', 'İzmir (ADB)', 'Trabzon (TZX)', '2026-11-28 16:00:00.000000', '2026-11-28 18:00:00.000000', 'A9', 150, 1900.00, 4),
  (5, 'TK520', 'İstanbul (IST)', 'Londra (LHR)', '2026-11-29 11:00:00.000000', '2026-11-29 13:50:00.000000', 'F12', 300, 6200.00, 0);

-- Biletler: 5 kayıt
INSERT INTO `Biletler` (`Id`, `UcusId`, `KullaniciId`, `KoltukNo`, `Fiyat`, `SatinAlmaTarihi`, `Durum`, `KartSahibi`, `KartTipi`, `KartSonDortHane`) VALUES
  (1, 1, 4, 1, 1450.00, '2026-11-25 00:00:00.000000', 0, NULL, NULL, NULL),
  (2, 2, 4, 2, 1200.00, '2026-11-24 00:00:00.000000', 0, NULL, NULL, NULL),
  (3, 2, 5, 3, 1200.00, '2026-11-23 00:00:00.000000', 0, NULL, NULL, NULL),
  (4, 3, 5, 4, 1650.00, '2026-11-22 00:00:00.000000', 0, NULL, NULL, NULL),
  (5, 5, 4, 5, 6200.00, '2026-11-21 00:00:00.000000', 0, NULL, NULL, NULL);

SET FOREIGN_KEY_CHECKS = 1;

-- =====================================================================
--  Örnek sorgular (uygulamadaki ekranların SQL karşılıkları)
-- =====================================================================

-- Uçuş bazında satılan bilet, doluluk ve gelir (Admin > Raporlar)
-- SELECT u.UcusNo,
--        CONCAT(u.Kalkis, ' -> ', u.Varis)        AS Guzergah,
--        COUNT(b.Id)                               AS SatilanBilet,
--        u.Kapasite,
--        ROUND(COUNT(b.Id) * 100 / u.Kapasite, 1)  AS DolulukYuzde,
--        IFNULL(SUM(b.Fiyat), 0)                   AS Gelir
-- FROM Ucuslar u
-- LEFT JOIN Biletler b ON b.UcusId = u.Id AND b.Durum = 0
-- GROUP BY u.Id, u.UcusNo, u.Kalkis, u.Varis, u.Kapasite
-- ORDER BY u.KalkisZamani;

-- Bir kullanıcının biletleri (Kullanıcı > Biletlerim)
-- SELECT k.AdSoyad, u.UcusNo, u.Kalkis, u.Varis, u.KalkisZamani, b.KoltukNo, b.Fiyat
-- FROM Biletler b
-- JOIN Kullanicilar k ON k.Id = b.KullaniciId
-- JOIN Ucuslar u      ON u.Id = b.UcusId
-- WHERE k.KullaniciAdi = 'kullanici'
-- ORDER BY b.SatinAlmaTarihi DESC;

-- Rollere göre kullanıcı sayısı
-- SELECT CASE Rol WHEN 0 THEN 'Admin' WHEN 1 THEN 'Personel' ELSE 'Kullanici' END AS RolAdi,
--        COUNT(*) AS Sayi
-- FROM Kullanicilar
-- GROUP BY Rol;
