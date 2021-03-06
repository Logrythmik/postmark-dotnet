﻿using System;
using System.Configuration;
using System.Linq;
using System.Net.Mail;
using NUnit.Framework;
using PostmarkDotNet;
using PostmarkDotNet.Validation;

namespace Postmark.Tests
{
    [TestFixture]
    public partial class PostmarkClientTests
    {
        [SetUp]
        public void SetUp()
        {
            var settings = ConfigurationManager.AppSettings;
            Assert.IsNotNull(
                settings,
                "You must include an 'app.config' file in your unit test project. See 'app.config.example'."
                );

            _serverToken = settings["ServerToken"];
            _from = settings["From"];
            _to = settings["To"];
        }

        private const string Subject = "A test from Postmark.NET";
        private const string TextBody = "This is a test message!";
        private const string InvalidRecipient = "test@mctesterton.com";

        private static string _serverToken;
        private static string _from;
        private static string _to;

        [Test]
        public void Can_detect_html_in_message()
        {
            var message = new PostmarkMessage(_from, _to, Subject, "Have a <b>great</b> day!");

            Assert.IsNotNull(message);
            Assert.IsTrue(IsBodyHtml(message));
        }

        [Test]
        public void Can_detect_plain_text_message()
        {
            var message = new PostmarkMessage(_from, _to, Subject, "Have a great day!");

            Assert.IsNotNull(message);
            Assert.IsFalse(IsBodyHtml(message));
        }

        private static bool IsBodyHtml(PostmarkMessage message)
        {
            return !string.IsNullOrEmpty(message.HtmlBody);
        }

        [Test]
        [Ignore("This test sends a real email.")]
        public void Can_send_message_with_token_and_signature()
        {
            var postmark = new PostmarkClient(_serverToken);

            var email = new PostmarkMessage
                            {
                                To = _to,
                                From = _from, // This must be a verified sender signature
                                Subject = Subject,
                                TextBody = TextBody
                            };

            var response = postmark.SendMessage(email);

            Assert.IsNotNull(response);
            Assert.IsNotNullOrEmpty(response.Message);
            Assert.IsTrue(response.Status == PostmarkStatus.Success);

            Console.WriteLine("Postmark -> " + response.Message);
        }

        [Test]
        [Ignore("This test sends a real email.")]
        public void Can_send_message_with_token_and_signature_and_name_based_email()
        {
            var postmark = new PostmarkClient(_serverToken);

            var email = new PostmarkMessage
            {
                To = _to,
                From = string.Format("The Team <{0}>", _from), // This must be a verified sender signature
                Subject = Subject,
                TextBody = TextBody
            };

            var response = postmark.SendMessage(email);

            Assert.IsNotNull(response);
            Assert.IsNotNullOrEmpty(response.Message);
            Assert.IsTrue(response.Status == PostmarkStatus.Success);

            Console.WriteLine("Postmark -> " + response.Message);
        }

        [Test]
        [Ignore("This test sends a real email.")]
        public void Can_send_message_with_token_and_signature_and_headers()
        {
            var postmark = new PostmarkClient(_serverToken);

            var email = new PostmarkMessage
            {
                To = _to,
                From = _from, // This must be a verified sender signature
                Subject = Subject,
                TextBody = TextBody,
            };

            email.Headers.Add("X-Header-Test-1", "This is a header value");
            email.Headers.Add("X-Header-Test-2", "This is another header value");

            var response = postmark.SendMessage(email);

            Assert.IsNotNull(response);
            Assert.IsNotNullOrEmpty(response.Message);
            Assert.IsTrue(response.Status == PostmarkStatus.Success);
            Assert.AreNotEqual(default(DateTime), response.SubmittedAt, "Missing submitted time value.");

            Console.WriteLine("Postmark -> " + response.Message);
        }

        [Test]
        [Ignore("This test sends a real email.")]
        public void Can_send_message_with_file_attachment()
        {
            var postmark = new PostmarkClient(_serverToken);

            var email = new PostmarkMessage
            {
                To = _to,
                From = _from, // This must be a verified sender signature
                Subject = Subject,
                TextBody = TextBody,
            };

            email.AddAttachment("logo.png", "image/png");
            
            var response = postmark.SendMessage(email);

            Assert.IsNotNull(response);
            Assert.IsNotNullOrEmpty(response.Message);
            Assert.IsTrue(response.Status == PostmarkStatus.Success);
            Console.WriteLine("Postmark -> " + response.Message);
        }

        [Test]
        [ExpectedException(typeof (ValidationException))]
        public void Can_send_message_with_token_and_signature_and_invalid_recipient_and_throw_validation_exception()
        {
            var postmark = new PostmarkClient(_serverToken);

            var email = new PostmarkMessage
                            {
                                To = "earth",
                                From = _from,
                                Subject = Subject,
                                TextBody = TextBody
                            };

            postmark.SendMessage(email);
        }

        [Test]
        public void Can_send_message_without_signature_and_receive_422()
        {
            var postmark = new PostmarkClient(_serverToken);

            var email = new PostmarkMessage
                            {
                                To = InvalidRecipient,
                                From = InvalidRecipient, // This must not be a verified sender signature
                                Subject = Subject,
                                TextBody = TextBody
                            };

            var response = postmark.SendMessage(email);

            Assert.IsNotNull(response);
            Assert.IsNotNullOrEmpty(response.Message);
            Assert.IsTrue(response.Status == PostmarkStatus.UserError);

            Console.WriteLine("Postmark -> " + response.Message);
        }

        [Test]
        public void Can_send_message_without_token_and_receive_401()
        {
            var postmark = new PostmarkClient("");

            var email = new PostmarkMessage
                            {
                                To = InvalidRecipient,
                                From = InvalidRecipient,
                                Subject = Subject,
                                TextBody = TextBody
                            };

            var response = postmark.SendMessage(email);

            Assert.IsNotNull(response);
            Assert.IsNotNullOrEmpty(response.Message);
            Assert.IsTrue(response.Status == PostmarkStatus.UserError);

            Console.WriteLine("Postmark -> " + response.Message);
        }

        [Test]
        public void Can_send_message_with_cc_and_bcc()
        {
            var postmark = new PostmarkClient("POSTMARK_API_TEST");

            var email = new PostmarkMessage
                            {
                                To = InvalidRecipient,
                                Cc = "test-cc@example.com",
                                Bcc = "test-bcc@example.com",
                                From = InvalidRecipient,
                                Subject = Subject,
                                TextBody = TextBody
                            };

            var response = postmark.SendMessage(email);

            Assert.IsNotNull(response);
            Assert.IsNotNullOrEmpty(response.Message);
            Assert.IsTrue(response.Status == PostmarkStatus.Success);

            Console.WriteLine("Postmark -> " + response.Message);
        }

        [Test]
        public void Can_generate_postmarkmessage_from_mailmessage()
        {
            var mm = new MailMessage
                         {
                             Subject = "test",
                             Body = "test"
                         };
            mm.Headers.Add ("X-PostmarkTag", "mytag");

            var pm = new PostmarkMessage (mm);
            Assert.AreEqual (mm.Subject, pm.Subject);
            Assert.AreEqual (mm.Body, pm.TextBody);
            Assert.AreEqual ("mytag", pm.Tag);
        }

        [Test]
        [Ignore("This test sends two real emails.")]
        public void Can_send_batched_messages()
        {
            var postmark = new PostmarkClient(_serverToken);

            var first = new PostmarkMessage
            {
                To = _to,
                From = _from, // This must be a verified sender signature
                Subject = Subject,
                TextBody = TextBody + " one"
            };
            var second = new PostmarkMessage
            {
                To = _to,
                From = _from, // This must be a verified sender signature
                Subject = Subject,
                TextBody = TextBody + " two"
            };

            var responses = postmark.SendMessages(first, second);
            Assert.AreEqual(2, responses.Count());

            foreach (var response in responses)
            {
                Assert.IsNotNull(response);
                Assert.IsNotNullOrEmpty(response.Message);
                Assert.IsTrue(response.Status == PostmarkStatus.Success);
                Console.WriteLine("Postmark -> " + response.Message);
            }
        }
    }
}