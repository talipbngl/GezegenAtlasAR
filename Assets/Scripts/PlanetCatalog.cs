using UnityEngine;

namespace GezegenAtlasi
{
    public sealed class PlanetInfo
    {
        public readonly string Name;
        public readonly string Eyebrow;
        public readonly string Description;
        public readonly string Highlight;
        public readonly Color Color;
        public readonly float Radius;
        public readonly float Orbit;
        public readonly float Angle;

        public PlanetInfo(string name, string eyebrow, string description, string highlight,
            string htmlColor, float radius, float orbit, float angle)
        {
            Name = name;
            Eyebrow = eyebrow;
            Description = description;
            Highlight = highlight;
            ColorUtility.TryParseHtmlString(htmlColor, out var parsed);
            Color = parsed;
            Radius = radius;
            Orbit = orbit;
            Angle = angle;
        }
    }

    public static class PlanetCatalog
    {
        // Görsel boyutlar ve yörüngeler sunum için düzenlenmiştir; astronomik ölçek değildir.
        public static readonly PlanetInfo[] Planets =
        {
            new PlanetInfo("Merkür", "01 / KARASAL", "Güneş'e en yakın ve sistemin en küçük gezegeni. İnce atmosferi nedeniyle gündüz ve gece arasında büyük sıcaklık farkları yaşanır.", "EN YAKIN GEZEGEN", "#B8A69B", .018f, .105f, 42f),
            new PlanetInfo("Venüs", "02 / KARASAL", "Kalın atmosferi ısıyı hapseder. Güneş'e Merkür'den daha uzak olmasına rağmen Güneş Sistemi'nin en sıcak gezegenidir.", "EN SICAK GEZEGEN", "#E9B978", .026f, .155f, 148f),
            new PlanetInfo("Dünya", "03 / KARASAL", "Yaşam olduğunu bildiğimiz tek gezegen. Sıvı su, atmosfer ve koruyucu manyetik alan burada bir araya gelir.", "EVİMİZ", "#58A7E5", .028f, .205f, 254f),
            new PlanetInfo("Mars", "04 / KARASAL", "Demir oksitli yüzeyi ona kızıl görünümünü verir. Robot gezginlerle ayrıntılı olarak araştırılan bir gezegendir.", "KIZIL GEZEGEN", "#DE7453", .023f, .255f, 321f),
            new PlanetInfo("Jüpiter", "05 / GAZ DEVİ", "Güneş Sistemi'nin en büyük gezegeni. Atmosferindeki bantlar ve Büyük Kırmızı Leke adı verilen uzun ömürlü fırtına dikkat çeker.", "EN BÜYÜK GEZEGEN", "#D9A678", .060f, .320f, 93f),
            new PlanetInfo("Satürn", "06 / GAZ DEVİ", "Buz ve kaya parçalarından oluşan görkemli halkalarıyla tanınır. Jüpiter'den sonra sistemin ikinci büyük gezegenidir.", "HALKALI DEV", "#E9CD98", .049f, .385f, 220f),
            new PlanetInfo("Uranüs", "07 / BUZ DEVİ", "Dönme ekseni çok eğiktir; bu yüzden yörüngesinde adeta yan yatmış biçimde döner. Soluk mavi rengi atmosferindeki metanla ilişkilidir.", "YAN YATAN GEZEGEN", "#A2DDE0", .038f, .445f, 338f),
            new PlanetInfo("Neptün", "08 / BUZ DEVİ", "Güneş'e en uzak gezegendir. Mavi atmosferi ve güçlü rüzgârlarıyla bilinir.", "EN UZAK GEZEGEN", "#477CDF", .037f, .505f, 155f)
        };
    }
}
