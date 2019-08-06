// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;
using System;
using Newtonsoft.Json.Linq;

namespace QnA
{
    public class QnABot : ActivityHandler
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<QnABot> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string[] _cards =
        {
            Path.Combine(".", "Resources", "CallCard.json")
        };

        public QnABot(IConfiguration configuration, ILogger<QnABot> logger, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {

            using (StreamWriter file = new StreamWriter(@"C:\Users\fvsa155\Desktop\Estagio\Stats\Stats.txt", false))
            {
                ;
            }

            var userName = turnContext.Activity.From.Name;
            var reply = MessageFactory.Text($"Bom dia, estou aqui para responder as suas duvidas sobre o Prisma.\r\n Por favor mantenha as perguntas o mais simples possiveis ou diga só as palavras chave.");
            await turnContext.SendActivityAsync(reply, cancellationToken);
        }
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var stats = new Test();
            var httpClient = _httpClientFactory.CreateClient();
            var cardAttachment = CreateAdaptiveCardAttachment(_cards[0]);
            var qnaOptions = new QnAMakerOptions();
            var qnaMaker = new QnAMaker(new QnAMakerEndpoint
            {
                KnowledgeBaseId = _configuration["QnAKnowledgebaseId"],
                EndpointKey = _configuration["QnAEndpointKey"],
                Host = _configuration["QnAEndpointHostName"]
            }, null, httpClient);
            _logger.LogInformation("Calling QnA Maker");

            qnaOptions.Top = 3;
            qnaOptions.ScoreThreshold = 0.01f;
            
            var response = await qnaMaker.GetAnswersAsync(turnContext, qnaOptions);

            if (response != null && response.Length > 0)
            {
                if (response[0].Score > 0.8f )
                {
                    if(response.Length==1)
                    {
                        await turnContext.SendActivityAsync(MessageFactory.Text(response[0].Answer), cancellationToken);
                    }
                    else if (response.Length > 1)
                    {
                        if (response[0].Score==1 && response[1].Score !=1)
                        {
                            await turnContext.SendActivityAsync(MessageFactory.Text(response[0].Answer), cancellationToken);
                        }
                        else
                        {
                            var reply = MessageFactory.SuggestedActions(
                            new CardAction[]
                            {
                                new CardAction(title: response[0].Questions[0], type: ActionTypes.ImBack, value: response[0].Questions[0]),
                                new CardAction( title: response[1].Questions[0], type: ActionTypes.ImBack, value: response[1].Questions[0]),
                                new CardAction( title: "Falar com um assisstente", type: ActionTypes.OpenUrl, value: "https://localhost:44375/" )
                            }, text: "Qual destas perguntas melhor representa o que pretende?");
                            await turnContext.SendActivityAsync(reply, cancellationToken);
                        }
                    }
                }
                else
                {
                    // var reply = MessageFactory.Text("Temos varias perguntas que podem ser o que pretende.");
                    string[] placeholder = new String[response.Length];
                    for (int i = 0; i < response.Length; i++)
                    {
                        placeholder[i] = response[i].Questions[0];
                    }
                    if (response.Length == 1)
                    {
                        await turnContext.SendActivityAsync(MessageFactory.Text(response[0].Answer), cancellationToken);
                    }
                    else if (response.Length == 2)
                    {
                        var reply = MessageFactory.SuggestedActions(
                        new CardAction[]
                        {
                                new CardAction(title: placeholder[0], type: ActionTypes.ImBack, value: placeholder[0]),
                                new CardAction( title: placeholder[1], type: ActionTypes.ImBack, value: placeholder[1]),
                                new CardAction( title: "Falar com um assisstente", type: ActionTypes.ImBack, value: "Falar com um assistente")
                        }, text: "Qual destas perguntas melhor representa o que pretende?");
                        await turnContext.SendActivityAsync(reply, cancellationToken);
                    }
                    else if (response.Length == 3)
                    {
                        var reply = MessageFactory.SuggestedActions(
                        new CardAction[]{
                                new CardAction(title: placeholder[0], type: ActionTypes.ImBack, value: placeholder[0]),
                                new CardAction(title: placeholder[1], type: ActionTypes.ImBack, value: placeholder[1]),
                                new CardAction(title: placeholder[2], type: ActionTypes.ImBack, value: placeholder[2]),
                                new CardAction(title: "Falar com um assisstente", type: ActionTypes.ImBack, value: "Falar com um assistente")
                        }, text: "Qual destas perguntas melhor representa o que pretende?");
                        await turnContext.SendActivityAsync(reply, cancellationToken);
                    }
                }   
            }
            else
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("Peço desculpa, mas não consegui entender a pergunta.\r\n Por favor pergunte outra vez com outras palavras, construa a pergunta o mais simples possível ou use palavras chave."), cancellationToken);  
            }
        }


        private static Attachment CreateAdaptiveCardAttachment(string filePath)
        {
            var adaptiveCardJson = File.ReadAllText(filePath);
            var adaptiveCardAttachment = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(adaptiveCardJson),
            };
            return adaptiveCardAttachment;
        }
    }
}
