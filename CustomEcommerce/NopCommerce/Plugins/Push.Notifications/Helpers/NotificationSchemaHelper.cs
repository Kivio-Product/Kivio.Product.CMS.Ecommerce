using System.Collections.Generic;
using DotnetGeminiSDK.Model.Request;

namespace Nop.Plugin.Misc.PushNotifications.Helpers
{
    public static class NotificationSchemaHelper
    {
        /// <summary>
        /// Creates a schema for push notification response with title and body
        /// </summary>
        /// <returns>ResponseSchema for push notification</returns>
        public static ResponseSchema CreatePushNotificationSchema()
        {
            return new ResponseSchema
            {
                Type = "OBJECT",
                Properties = new Dictionary<string, SchemaProperty>
                {
                    ["title"] = new SchemaProperty
                    {
                        Type = "STRING",
                        Description = "The title of the push notification"
                    },
                    ["body"] = new SchemaProperty
                    {
                        Type = "STRING",
                        Description = "The body text of the push notification"
                    }
                },
                Required = new List<string> { "title", "body" },
                PropertyOrdering = new List<string> { "title", "body" }
            };
        }

        /// <summary>
        /// Creates a schema for product notification with additional product details
        /// </summary>
        /// <returns>ResponseSchema for product notification</returns>
        public static ResponseSchema CreatePushEmojiNotificationSchema()
        {
            return new ResponseSchema
            {
                Type = "OBJECT",
                Properties = new Dictionary<string, SchemaProperty>
                {
                    ["title"] = new SchemaProperty
                    {
                        Type = "STRING",
                        Description = "The catchy title of the push notification"
                    },
                    ["body"] = new SchemaProperty
                    {
                        Type = "STRING",
                        Description = "The compelling body text of the push notification"
                    },
                    ["emoji"] = new SchemaProperty
                    {
                        Type = "STRING",
                        Description = "A relevant emoji for the notification"
                    }
                },
                Required = new List<string> { "title", "body" },
                PropertyOrdering = new List<string> { "title", "body", "emoji" }
            };
        }

        /// <summary>
        /// Creates a schema for category notification
        /// </summary>
        /// <returns>ResponseSchema for category notification</returns>
        public static ResponseSchema CreateCategoryNotificationSchema()
        {
            return new ResponseSchema
            {
                Type = "OBJECT",
                Properties = new Dictionary<string, SchemaProperty>
                {
                    ["title"] = new SchemaProperty
                    {
                        Type = "STRING",
                        Description = "The engaging title for the category notification"
                    },
                    ["body"] = new SchemaProperty
                    {
                        Type = "STRING",
                        Description = "The persuasive body text for the category notification"
                    },
                    ["callToAction"] = new SchemaProperty
                    {
                        Type = "STRING",
                        Description = "A short call-to-action phrase"
                    },
                    ["emoji"] = new SchemaProperty
                    {
                        Type = "STRING",
                        Description = "A relevant emoji for the category notification"
                    }
                },
                Required = new List<string> { "title", "body" },
                PropertyOrdering = new List<string> { "title", "body", "callToAction", "emoji" }
            };
        }
    }
}
