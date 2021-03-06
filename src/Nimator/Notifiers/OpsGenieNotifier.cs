﻿using System;
using Nimator.Settings;
using Nimator.Util;

namespace Nimator.Notifiers
{
    internal class OpsGenieNotifier : INotifier
    {
        private const int MaxOpsgenieTagLength = 50;
        private const int MaxOpsgenieMessageLength = 130;
        private const string AlertUrl = "https://api.opsgenie.com/v1/json/alert";
        private const string HeartbeatUrl = "https://api.opsgenie.com/v1/json/heartbeat/send";
        private readonly OpsGenieSettings settings;

        public OpsGenieNotifier(OpsGenieSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (string.IsNullOrWhiteSpace(settings.ApiKey)) throw new ArgumentException("settings.ApiKey was not set", nameof(settings));
            if (string.IsNullOrWhiteSpace(settings.HeartbeatName)) throw new ArgumentException("settings.HeartbeatName was not set", nameof(settings));
            if (string.IsNullOrWhiteSpace(settings.TeamName)) throw new ArgumentException("settings.TeamName was not set", nameof(settings));

            this.settings = settings;
        }
        
        public void Notify(INimatorResult result)
        {
            SendHeartbeat();

            if (result.Level >= settings.Threshold)
            {
                NotifyFailureResult(result);
            }
        }

        private void SendHeartbeat()
        {
            var request = new OpsGenieHeartbeatRequest(this.settings.ApiKey, this.settings.HeartbeatName);
            SimpleRestUtils.PostToRestApi(HeartbeatUrl, request);
        }

        private void NotifyFailureResult(INimatorResult result)
        {
            var failingLayerName = (result.GetFirstFailedLayerName() ?? "UnknownLayer").Truncate(MaxOpsgenieTagLength);
            var message = result.Message.Truncate(MaxOpsgenieMessageLength);

            var request = new OpsGenieCreateAlertRequest(this.settings.ApiKey, message)
            {
                alias = "nimator-failure",
                description = result.RenderPlainText(),
                teams = new[] { this.settings.TeamName },
                tags = new[] { "Nimator", failingLayerName }
            };

            SimpleRestUtils.PostToRestApi(AlertUrl, request);
        }
    }
}
