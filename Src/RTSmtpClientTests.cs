using System.Net.Mail;
using NUnit.Framework;

namespace RT.Util
{
    [TestFixture]
    public sealed class RTSmtpClientTests
    {
        [Test]
        public void TestEncodeHeader()
        {
            test(
                RTSmtpClient.EncodeHeader("Subject", "Test subject"),
                @"Subject: Test subject");
            test(
                RTSmtpClient.EncodeHeader("Subject", "Lorem ipsum dolor sit amet, consectetuer adipiscing elit. Aenean commodo ligula eget dolor. Aenean massa. Lorem ipsum dolor sit amet, consectetuer adipiscing elit. Aenean commodo ligula eget dolor."),
                @"Subject: Lorem ipsum dolor sit amet, consectetuer adipiscing elit. Aenean
 commodo ligula eget dolor. Aenean massa. Lorem ipsum dolor sit amet,
 consectetuer adipiscing elit. Aenean commodo ligula eget dolor.");
            test(
                RTSmtpClient.EncodeHeader("Subject", "Далеко-далеко за словесными горами в стране гласных и согласных живут рыбные тексты. Вдали от всех живут они в буквенных домах на берегу Семантика большого языкового океана."),
                @"Subject: =?UTF-8?B?0JTQsNC70LXQutC+LdC00LDQu9C10LrQviDQt9CwINGB0LvQvtCy?=
 =?UTF-8?B?0LXRgdC90YvQvNC4INCz0L7RgNCw0LzQuCDQsiDRgdGC0YDQsNC90LUg0LM=?=
 =?UTF-8?B?0LvQsNGB0L3Ri9GFINC4INGB0L7Qs9C70LDRgdC90YvRhSDQttC40LLRg9GC?=
 =?UTF-8?B?INGA0YvQsdC90YvQtSDRgtC10LrRgdGC0YsuINCS0LTQsNC70Lgg0L7RgiA=?=
 =?UTF-8?B?0LLRgdC10YUg0LbQuNCy0YPRgiDQvtC90Lgg0LIg0LHRg9C60LLQtdC90L0=?=
 =?UTF-8?B?0YvRhSDQtNC+0LzQsNGFINC90LAg0LHQtdGA0LXQs9GDINCh0LXQvNCw0L0=?=
 =?UTF-8?B?0YLQuNC60LAg0LHQvtC70YzRiNC+0LPQviDRj9C30YvQutC+0LLQvtCz0L4g?=
 =?UTF-8?B?0L7QutC10LDQvdCwLg==?=");
            test(
                RTSmtpClient.EncodeHeader("Subject", "sjfwoijfdalwfdwiufdhawldkjhkawuydgakwjdfygakwjdfybawkdufyghawkjdfgawkfuygawkjdfygawkjdybfawkuydgawkudyg awdfgkawuydgawdfawdfygliuhawdlufhawlydfglawyeghdliuhewgdliuhadgifaliugawdfliyguawdlfygawdflygawdlfguy sd"),
                @"Subject:
 sjfwoijfdalwfdwiufdhawldkjhkawuydgakwjdfygakwjdfybawkdufyghawkjdfgawkfuygawkjdfygawkjdybfawkuydgawkudyg
 awdfgkawuydgawdfawdfygliuhawdlufhawlydfglawyeghdliuhewgdliuhadgifaliugawdfliyguawdlfygawdflygawdlfguy
 sd");

            test(
                RTSmtpClient.EncodeHeader("From", new MailAddress("test@example.com")),
                "From: test@example.com");
            test(
                RTSmtpClient.EncodeHeader("From", new MailAddress("test@example.com"), new MailAddress("test@example.com"), new MailAddress("test@example.com"), new MailAddress("test@example.com"), new MailAddress("test@example.com"), new MailAddress("test@example.com")),
                @"From: test@example.com, test@example.com, test@example.com,
 test@example.com, test@example.com, test@example.com");
            test(
                RTSmtpClient.EncodeHeader("From", new MailAddress("Test <test@example.com>"), new MailAddress("\"Τεστ Σμιθ\" <test@example.com>"), new MailAddress("test@example.com"), new MailAddress("test@example.com"), new MailAddress("\"Грегор Замза обнаружил что он у себя в постели\" <test@example.com>"), new MailAddress("test@example.com")),
                @"From: ""Test"" <test@example.com>, ""=?UTF-8?B?zqTOtc+Dz4QgzqPOvM65zrg=?=""
 <test@example.com>, test@example.com, test@example.com, ""=?UTF-8?B?0JM=?=
 =?UTF-8?B?0YDQtdCz0L7RgCDQl9Cw0LzQt9CwINC+0LHQvdCw0YDRg9C20LjQuyDRh9GC?=
 =?UTF-8?B?0L4g0L7QvSDRgyDRgdC10LHRjyDQsiDQv9C+0YHRgtC10LvQuA==?=""
 <test@example.com>, test@example.com");
        }

        private void test(string actual, string expected)
        {
            Assert.AreEqual(expected, actual);
        }
    }
}
