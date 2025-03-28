using Alexa.NET;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Alexa.NET.Response;
using Alexa.NET.Response.Directive;
using Alexa.NET.Response.Directive.Templates;
using Alexa.NET.Response.Directive.Templates.Types;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Twilio;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using JsonSerializer = Amazon.Lambda.Serialization.Json.JsonSerializer;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(JsonSerializer))]

namespace SR_AWSLambda
{
    public class Function
    {
        private static HttpClient _httpClient;

        private const string BackgroundImageSource =
            "https://fypj-smartreceptionist.com/assets/images/backgrounds/sit-bg.png";

        private const string Url = "https://fypj-smartreceptionist.com";
        private const string InvocationName = "hello";

        public Function()
        {
            _httpClient = new HttpClient();
        }

        public async Task<SkillResponse> FunctionHandler(SkillRequest input, ILambdaContext context)
        {
            PlainTextOutputSpeech outputSpeech;
            switch (input.Request)
            {
                case LaunchRequest _:
                    return GetTasksResponse(
                        "Welcome to N. Y. P. School of Information Technology's Smart Receptionist. " +
                        "You can talk to me and I can help you with tasks like registering your visit with a staff. " +
                        "How can I help you now?");
                case IntentRequest intentRequest when intentRequest.Intent.Name.Equals("RegisterVisitIntent"):
                    outputSpeech = new PlainTextOutputSpeech();
                    if (intentRequest.DialogState != DialogState.Completed)
                    {
                        var slot = intentRequest.Intent.Slots["staff"];
                        var textInfo = new CultureInfo("en-US", false).TextInfo;
                        var staffRequested = textInfo.ToTitleCase(slot.Value);
                        var confirmSlotWithCard =
                            await GetRegisterVisitResponse(input, context, slot, staffRequested, outputSpeech);
                        if (confirmSlotWithCard != null) return confirmSlotWithCard;
                    }

                    break;
                case IntentRequest intentRequest when intentRequest.Intent.Name.Equals("FindMeetingRoomIntent"):
                {
                    var meetingRoom = intentRequest.Intent.Slots["meetingRoom"].Value;
                    bool result = await GetMeetingRoomResultAsync(meetingRoom, context);
                    if (!result)
                    {
                        context.Logger.LogLine("The meeting room was not found.");
                        return MakeSkillResponse(
                            "Sorry, I couldn\'t find the meeting room you were asking for. Please ask again.",
                            false);
                    }

                    List<DirectionsStep> directionsSteps =
                        await GetDirectionsStepsAsync(meetingRoom, context);
                    if (!directionsSteps.Any())
                    {
                        context.Logger.LogLine("The meeting room does not have any directions configured yet.");
                        return MakeSkillResponse(
                            "Sorry, I don\'t have any directions configured for that meeting room yet. " +
                            $"Please say, \"Alexa, ask {InvocationName} to display the floor plan\" to see where " +
                            $"{Regex.Replace(meetingRoom, ".{1}(?!$)", "$0 ")} is.",
                            true);
                    }

                    var steps = directionsSteps.Select(s => s.StepInstructions).ToList();
                    var lastIndex = directionsSteps.Count - 1;
                    var lastStep = directionsSteps[lastIndex].StepInstructions;
                    lastStep = Regex.Replace(lastStep, "your ", "", RegexOptions.IgnoreCase);
                    directionsSteps[lastIndex].StepInstructions =
                        lastStep.First().ToString().ToUpper() + lastStep.Substring(1);
                    var items = new List<ListItem>();
                    var id = 1;
                    foreach (var t in directionsSteps)
                    {
                        var item = new ListItem
                        {
                            Token = $"item_{id}",
                            Content = new TemplateContent
                            {
                                Primary = new TemplateText
                                {
                                    Text = t.StepAction,
                                    Type = TextType.Plain
                                },
                                Secondary = new TemplateText
                                {
                                    Text = t.StepInstructions,
                                    Type = TextType.Plain
                                }
                            },
                            Image = new TemplateImage
                            {
                                Sources = new List<ImageSource>
                                {
                                    new ImageSource
                                    {
                                        Url = Url + t.StepActionImage
                                    }
                                },
                                ContentDescription = t.ContentDescription
                            }
                        };
                        items.Add(item);
                        id++;
                    }

                    var actual = new ListTemplate2
                    {
                        Token = "list_template_two",
                        BackgroundImage = new TemplateImage
                        {
                            ContentDescription = "NYP SIT",
                            Sources = new List<ImageSource>
                            {
                                new ImageSource {Url = BackgroundImageSource}
                            }
                        },
                        Title = $"Directions for \"{meetingRoom}\"",
                        Items = items
                    };
                    var response =
                        ResponseBuilder.Tell($"OK, from the receptionist counter, please {GetStepsResponse(steps)}.");
                    response.Response.Directives.Add(new DisplayRenderTemplateDirective {Template = actual});
                    return response;
                }
                case IntentRequest intentRequest when intentRequest.Intent.Name.Equals("DisplayFloorPlanIntent"):
                {
                    var actual = CreateBodyTemplate7("Block L Level 3", "Block L Level 3",
                        "/assets/images/blk-l-level-3.jpg");
                    var response =
                        ResponseBuilder.Tell("OK, the floor plan of block L. level 3 is displayed on the screen.");
                    response.Response.Directives.Add(new DisplayRenderTemplateDirective {Template = actual});
                    return response;
                }
                case IntentRequest intentRequest when intentRequest.Intent.Name.Equals("ReportLostItemIntent"):
                    if (intentRequest.DialogState != DialogState.Completed)
                    {
                        outputSpeech = new PlainTextOutputSpeech();
                        return await ElicitSlotWithCard(intentRequest.Intent, outputSpeech, null, input, context);
                    }

                    break;
                case IntentRequest intentRequest when intentRequest.Intent.Name.Equals("HandOverFoundItemIntent"):
                    var progressive = new ProgressiveResponse(input);
                    if (progressive.CanSend())
                    {
                        await progressive.SendSpeech("OK, please wait while I check if any staff is available.");
                    }

                    Staff staffResult = await GetStaffResultAsync(context);
                    if (staffResult == null)
                    {
                        context.Logger.LogLine("There were no staff available.");
                        return MakeSkillResponse(
                            "Sorry, there's currently no staff available now. Please try again later.", true);
                    }

                    if (progressive.CanSend())
                    {
                        var speechResponse =
                            $"OK, please say, \"Alexa, drop in on {staffResult.EchoDeviceName}\" to connect with a staff. " +
                            "To hang up, say, \"Alexa, hang up.\"";
                        var response = ResponseBuilder.TellWithCard(speechResponse, "Lost and Found", speechResponse);
                        return response;
                    }

                    break;
                case IntentRequest intentRequest when intentRequest.Intent.Name.Equals("SubmitStatementOfAbsenceIntent"):
                {
                    var actual = GetSubmitStatementOfAbsenceListTemplate1();
                    var response =
                        ResponseBuilder.Tell(
                            "To submit your statement of absence, follow the instructions as shown on the screen. " +
                            "Click on each of the list items or say the item number to learn more.");
                    response.Response.Directives.Add(new DisplayRenderTemplateDirective {Template = actual});
                    response.Response.ShouldEndSession = false;
                    return response;
                }
                case IntentRequest intentRequest when intentRequest.Intent.Name.Equals("ElementSelectedIntent"):
                    var numberValue = intentRequest.Intent.Slots["numberValue"].Value;
                    return GetElementSelectedResponse(numberValue);
                case IntentRequest intentRequest when intentRequest.Intent.Name.Equals("AMAZON.HelpIntent"):
                    return GetTasksResponse(
                        "I can help you with tasks like registering your visit with a staff, find a meeting room, " +
                        "report a lost item or hand over a found item, or submit your statement of absence. " +
                        "Below each task displayed is a phrase which you can say to me. How can I help you now?");
                case IntentRequest intentRequest when intentRequest.Intent.Name.Equals("AMAZON.PreviousIntent"):
                {
                    var actual = GetSubmitStatementOfAbsenceListTemplate1();
                    var response = ResponseBuilder.Empty();
                    response.Response.Directives.Add(new DisplayRenderTemplateDirective {Template = actual});
                    response.Response.ShouldEndSession = false;
                    return response;
                }
                case DisplayElementSelectedRequest element:
                    var token = element.Token;
                    return GetElementSelectedResponse(token);
            }

            return MakeSkillResponse(
                $"I don't know how to handle this intent. Please say something like \"Alexa, ask {InvocationName} what can it do.\"",
                true);
        }

        #region Helpers
        private static SkillResponse MakeSkillResponse(string outputSpeech,
            bool shouldEndSession,
            string repromptText = "Just say, \"I have a meeting with John.\" To exit, say, \"Exit.\"")
        {
            var response = new ResponseBody
            {
                ShouldEndSession = shouldEndSession,
                OutputSpeech = new PlainTextOutputSpeech {Text = outputSpeech}
            };

            if (repromptText != null)
            {
                response.Reprompt = new Reprompt {OutputSpeech = new PlainTextOutputSpeech {Text = repromptText}};
            }

            var skillResponse = new SkillResponse
            {
                Response = response,
                Version = "1.0"
            };
            return skillResponse;
        }

        private static SkillResponse GetTasksResponse(string speechResponse)
        {
            var items = new List<ListItem>();
            var id = 1;
            var tasks = new List<string[]>
            {
                new[]
                {
                    "Register your visit", "Find a meeting room", "Lost and Found",
                    "Submit your statement of absence"
                },
                new[]
                {
                    "I have a meeting with ...", "Where is ...", "I lost an item/I found an item",
                    "I want to submit my S.O.A."
                }
            };
            for (int i = 0; i < tasks[0].Length; i++)
            {
                var item = new ListItem
                {
                    Token = $"item_{id}",
                    Content = new TemplateContent
                    {
                        Primary = new TemplateText
                        {
                            Text = $"<font size='5'>{tasks[0][i]}</font>",
                            Type = TextType.Rich
                        },
                        Secondary = new TemplateText
                        {
                            Text = tasks[1][i],
                            Type = TextType.Plain
                        }
                    }
                };
                items.Add(item);
                id++;
            }

            var actual = new ListTemplate1
            {
                Token = "list_template_one",
                BackgroundImage = new TemplateImage
                {
                    ContentDescription = "NYP SIT",
                    Sources = new List<ImageSource>
                    {
                        new ImageSource {Url = BackgroundImageSource}
                    }
                },
                Title = "Here are the tasks I can help you with:",
                Items = items
            };
            var response = ResponseBuilder.Tell(speechResponse);
            response.Response.Directives.Add(new DisplayRenderTemplateDirective { Template = actual });
            response.Response.ShouldEndSession = false;
            return response;
        }

        private static async Task<SkillResponse> GetRegisterVisitResponse(SkillRequest input, ILambdaContext context,
            Slot slot, string staffRequested, PlainTextOutputSpeech outputSpeech)
        {
            if (slot.ConfirmationStatus != ConfirmationStatus.Confirmed)
            {
                if (slot.ConfirmationStatus != ConfirmationStatus.Denied)
                {
                    List<Staff> staff = await GetStaffAsync(staffRequested, context);
                    if (!staff.Any())
                    {
                        context.Logger.LogLine("The staff was not found.");
                        outputSpeech.Text = "Sorry, I couldn't find the staff you were asking for. Please ask again.";
                        var response = ResponseBuilder.DialogElicitSlot(outputSpeech, "staff");
                        response.Response.Reprompt = new Reprompt
                        {
                            OutputSpeech = new PlainTextOutputSpeech { Text = "Which staff do you have a meeting with?" }
                        };
                        response.Response.Card = SimpleCard("Register your visit", outputSpeech.Text);
                        return response;
                    }

                    if (staff.Count > 1)
                    {
                        outputSpeech.Text = $"Which {staffRequested} do you have a meeting with?";
                        var response = ResponseBuilder.DialogElicitSlot(outputSpeech, "staff");
                        response.Response.Reprompt = new Reprompt { OutputSpeech = outputSpeech };
                        response.Response.Card = SimpleCard("Register your visit", outputSpeech.Text);
                        return response;
                    }

                    if (!string.Equals(staff[0].Name, staffRequested, StringComparison.CurrentCultureIgnoreCase))
                    {
                        outputSpeech.Text = $"You have a meeting with {staff[0].Name}, right?";
                        return ConfirmSlotWithCard(outputSpeech, slot, "Confirm Staff Requested");
                    }

                    slot.ConfirmationStatus = ConfirmationStatus.Confirmed;
                    var registerVisitResponse =
                        await GetStaffAvailabilityResponse(input, context, staffRequested, staff);
                    if (registerVisitResponse != null) return registerVisitResponse;
                }
                else
                {
                    outputSpeech.Text = "OK, who do you have a meeting with again?";
                    var response = ResponseBuilder.DialogElicitSlot(outputSpeech, "staff");
                    response.Response.Reprompt = new Reprompt
                    {
                        OutputSpeech = new PlainTextOutputSpeech { Text = "Who do you have a meeting with again?" }
                    };
                    var elicitSlotWithCard = SimpleCard("Register your visit", outputSpeech.Text);
                    response.Response.Card = elicitSlotWithCard;
                    return response;
                }
            }
            else
            {
                List<Staff> staff = await GetStaffAsync(staffRequested, context);
                var registerVisitResponse = await GetStaffAvailabilityResponse(input, context, staffRequested, staff);
                if (registerVisitResponse != null) return registerVisitResponse;
            }

            return null;
        }

        private static async Task<SkillResponse> GetStaffAvailabilityResponse(SkillRequest input,
            ILambdaContext context, string staffRequested, IReadOnlyList<Staff> staff)
        {
            if (staff[0].Status == "Available")
            {
                var speechResponse =
                    $"OK, please say, \"Alexa, drop in on {staff[0].EchoDeviceName}\" to connect with {staffRequested}. " +
                    "To hang up, say, \"Alexa, hang up.\"";
                var response = ResponseBuilder.TellWithCard(speechResponse, "Register your visit", speechResponse);
                return response;
            }

            var progressive = new ProgressiveResponse(input);
            if (progressive.CanSend())
            {
                await progressive.SendSpeech($"Sorry, {staffRequested} is currently {staff[0].Status.ToLower()}.");
            }

            if (progressive.CanSend())
            {
                bool result = await SendSms(staff[0].PhoneNumber, context);
                return MakeSkillResponse(
                    result
                        ? $"I've sent {staffRequested} a message about your visit."
                        : $"Sorry, I couldn't sent {staffRequested} a message about your visit.", true);
            }

            return null;
        }

        private static async Task<List<Staff>> GetStaffAsync(string staffName, ILambdaContext context)
        {
            var staff = new List<Staff>();
            var uri = new Uri($"{Url}/api/staff/{staffName}");
            context.Logger.LogLine($"Attempting to fetch data from {uri.AbsoluteUri}");
            try
            {
                var response = await _httpClient.GetStringAsync(uri);
                context.Logger.LogLine($"Response from URL:\n{response}");
                // TODO: (PMO) Handle bad requests
                staff = JsonConvert.DeserializeObject<List<Staff>>(response);
            }
            catch (Exception e)
            {
                context.Logger.LogLine($"Exception: {e.Message}");
                context.Logger.LogLine($"Stack Trace: {e.StackTrace}");
            }

            return staff;
        }

        private static async Task<bool> SendSms(string phoneNumber, ILambdaContext context)
        {
            var accountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID");
            var authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN");

            TwilioClient.Init(accountSid, authToken);

            try
            {
                await MessageResource.CreateAsync(
                    new PhoneNumber($"+65{phoneNumber}"),
                    from: new PhoneNumber("+17738325915"),
                    body: "Hello, you have a visitor waiting for you at the receptionist counter.");
                return true;
            }
            catch (ApiException e)
            {
                context.Logger.LogLine(e.Message);
                context.Logger.LogLine($"Twilio Error {e.Code} - {e.MoreInfo}");
            }

            return false;
        }

        private static async Task<bool> GetMeetingRoomResultAsync(string meetingRoom, ILambdaContext context)
        {
            var result = false;
            var uri = new Uri($"{Url}/api/FloorDirectory/{meetingRoom}");
            context.Logger.LogLine($"Attempting to fetch data from {uri.AbsoluteUri}");
            try
            {
                var response = await _httpClient.GetStringAsync(uri);
                context.Logger.LogLine($"Response from URL:\n{response}");
                // TODO: (PMO) Handle bad requests
                result = JsonConvert.DeserializeObject<bool>(response);
            }
            catch (Exception e)
            {
                context.Logger.LogLine($"Exception: {e.Message}");
                context.Logger.LogLine($"Stack Trace: {e.StackTrace}");
            }

            return result;
        }

        private static async Task<List<DirectionsStep>> GetDirectionsStepsAsync(string meetingRoom,
            ILambdaContext context)
        {
            var directionsSteps = new List<DirectionsStep>();
            var uri = new Uri($"{Url}/api/DirectionsSteps/{meetingRoom}");
            context.Logger.LogLine($"Attempting to fetch data from {uri.AbsoluteUri}");
            try
            {
                var response = await _httpClient.GetStringAsync(uri);
                context.Logger.LogLine($"Response from URL:\n{response}");
                // TODO: (PMO) Handle bad requests
                directionsSteps = JsonConvert.DeserializeObject<List<DirectionsStep>>(response);
            }
            catch (Exception e)
            {
                context.Logger.LogLine($"Exception: {e.Message}");
                context.Logger.LogLine($"Stack Trace: {e.StackTrace}");
            }

            return directionsSteps;
        }

        private static string GetStepsResponse(IReadOnlyList<string> steps)
        {
            var numberOfSteps = steps.Count;
            if (numberOfSteps > 1)
            {
                var needsOxfordComma = numberOfSteps > 2;
                var lastStepConjunction = (needsOxfordComma ? "," : "") + " and ";
                var lastStep = lastStepConjunction + steps[steps.Count - 1];
                return (string.Join(", ", steps.SkipLast(1)) + lastStep).ToLower();
            }

            return string.Join("", steps).ToLower();
        }

        private static async Task<SkillResponse> ElicitSlotWithCard(Intent intent, PlainTextOutputSpeech outputSpeech,
            string slotToElicit, SkillRequest input, ILambdaContext context, Intent updatedIntent = null)
        {
            var cardTitle = "Lost and Found";
            var reprompt = new Reprompt();
            if (string.IsNullOrEmpty(intent.Slots["name"].Value))
            {
                outputSpeech.Text = "What's your name?";
                reprompt.OutputSpeech = outputSpeech;
                slotToElicit = "name";
            }
            else if (string.IsNullOrEmpty(intent.Slots["phoneNumber"].Value))
            {
                outputSpeech.Text = "What's your phone number?";
                reprompt.OutputSpeech = outputSpeech;
                slotToElicit = "phoneNumber";
            }
            else if (!Regex.IsMatch(intent.Slots["phoneNumber"].Value, @"^[89][0-9]{7}$"))
            {
                outputSpeech.Text = "Sorry, the phone number you've specified is invalid. Please try again.";
                reprompt.OutputSpeech = new PlainTextOutputSpeech {Text = "What's your phone number?"};
                slotToElicit = "phoneNumber";
                cardTitle = "Invalid Phone Number";
            }
            else if (string.IsNullOrEmpty(intent.Slots["itemDescription"].Value))
            {
                outputSpeech.Text = "Please describe the item.";
                reprompt.OutputSpeech = outputSpeech;
                slotToElicit = "itemDescription";
            }
            else
            {
                var slot = intent.Slots["itemDescription"];
                if (slot.ConfirmationStatus != ConfirmationStatus.Confirmed)
                {
                    if (slot.ConfirmationStatus != ConfirmationStatus.Denied)
                    {
                        outputSpeech.Text = $"You described the item as {slot.Value}, right?";
                        cardTitle = "Confirm Item Description";
                        return ConfirmSlotWithCard(outputSpeech, slot, cardTitle);
                    }

                    outputSpeech.Text = "OK, please describe the item again.";
                    reprompt.OutputSpeech = new PlainTextOutputSpeech {Text = "Please describe the item again."};
                    slotToElicit = slot.Name;
                }
                else if (string.IsNullOrEmpty(intent.Slots["locationLost"].Value))
                {
                    outputSpeech.Text = "Where did you lose the item?";
                    reprompt.OutputSpeech = outputSpeech;
                    slotToElicit = "locationLost";
                }
                else
                {
                    slot = intent.Slots["locationLost"];
                    if (slot.ConfirmationStatus != ConfirmationStatus.Confirmed)
                    {
                        if (slot.ConfirmationStatus != ConfirmationStatus.Denied)
                        {
                            outputSpeech.Text = $"You lost the item at {slot.Value}, right?";
                            cardTitle = "Confirm Location Lost";
                            return ConfirmSlotWithCard(outputSpeech, slot, cardTitle);
                        }

                        outputSpeech.Text = "OK, where did you lose the item again?";
                        reprompt.OutputSpeech = new PlainTextOutputSpeech {Text = "Where did you lose the item again?"};
                        slotToElicit = slot.Name;
                    }
                    else if (string.IsNullOrEmpty(intent.Slots["dateLost"].Value))
                    {
                        outputSpeech.Text = "When did you lose the item?";
                        reprompt.OutputSpeech = outputSpeech;
                        slotToElicit = "dateLost";
                    }
                    else
                    {
                        var dateLost = Convert.ToDateTime(intent.Slots["dateLost"].Value);
                        if (dateLost > DateTime.Today)
                            intent.Slots["dateLost"].Value = dateLost.AddYears(-1).ToString("yyyy-MM-dd");
                        updatedIntent = intent;
                        if (string.IsNullOrEmpty(intent.Slots["timeLost"].Value))
                        {
                            outputSpeech.Text = "Around what time did you lose the item?";
                            reprompt.OutputSpeech = outputSpeech;
                            slotToElicit = "timeLost";
                        }
                        else
                        {
                            if (intent.ConfirmationStatus != ConfirmationStatus.Confirmed)
                            {
                                if (intent.ConfirmationStatus != ConfirmationStatus.Denied)
                                {
                                    return ConfirmIntentWithCard(intent);
                                }

                                intent.ConfirmationStatus = ConfirmationStatus.None;
                                foreach (var slotName in intent.Slots.Keys)
                                {
                                    var intentSlot = intent.Slots[slotName];
                                    if (intentSlot.ConfirmationStatus != ConfirmationStatus.Confirmed)
                                    {
                                        intentSlot.Value = string.Empty;
                                    }
                                }

                                updatedIntent = intent;
                                outputSpeech.Text = "OK, let me start over. What's your name?";
                                slotToElicit = "name";
                            }
                            else
                            {
                                var makeSkillResponse = await HandleReportLostItemIntent(intent, input, context);
                                if (makeSkillResponse != null) return makeSkillResponse;
                            }
                        }
                    }
                }
            }

            var response = updatedIntent != null
                ? ResponseBuilder.DialogElicitSlot(outputSpeech, slotToElicit, updatedIntent)
                : ResponseBuilder.DialogElicitSlot(outputSpeech, slotToElicit);
            response.Response.Reprompt = reprompt;
            var elicitSlotWithCard = SimpleCard(cardTitle, outputSpeech.Text);
            response.Response.Card = elicitSlotWithCard;
            return response;
        }

        private static SkillResponse ConfirmSlotWithCard(PlainTextOutputSpeech outputSpeech, Slot slot,
            string cardTitle)
        {
            var response = ResponseBuilder.DialogConfirmSlot(outputSpeech, slot.Name);
            response.Response.Reprompt = new Reprompt {OutputSpeech = outputSpeech};
            var confirmSlotWithCard = SimpleCard(cardTitle, outputSpeech.Text);
            response.Response.Card = confirmSlotWithCard;
            return response;
        }

        private static SkillResponse ConfirmIntentWithCard(Intent intent)
        {
            var (name, phoneNumber, dateLost, formattedDateLost, formattedTimeLost) = FormatSlotValues(intent);
            var ssmlOutputSpeech = new SsmlOutputSpeech
            {
                Ssml =
                    $"<speak>You said your name is {name}, " +
                    $"your phone number is <say-as interpret-as=\"digits\">{phoneNumber}</say-as>, " +
                    $"and you lost the item on {dateLost}, around {formattedTimeLost}, right?</speak>"
            };
            var response = ResponseBuilder.DialogConfirmIntent(ssmlOutputSpeech);
            var confirmIntentWithCard =
                SimpleCard("Lost and Found Summary",
                    $"You said your name is {name}, your phone number is {phoneNumber}, " +
                    $"and you lost the item on {formattedDateLost}, around {formattedTimeLost}, right?");
            response.Response.Card = confirmIntentWithCard;
            return response;
        }

        private static (string name, string phoneNumber, string dateLost, string formattedDateLost, string formattedTimeLost)
            FormatSlotValues(Intent intent)
        {
            var textInfo = new CultureInfo("en-US", false).TextInfo;
            var name = textInfo.ToTitleCase(intent.Slots["name"].Value);
            var phoneNumber = intent.Slots["phoneNumber"].Value;
            var dateLost = intent.Slots["dateLost"].Value;
            var formattedDateLost = Convert.ToDateTime(dateLost).ToString("MMM d, yyyy");
            var timeLost = intent.Slots["timeLost"].Value;
            string formattedTimeLost;
            switch (timeLost)
            {
                case "NI":
                    formattedTimeLost = "night";
                    break;
                case "MO":
                    formattedTimeLost = "morning";
                    break;
                case "AF":
                    formattedTimeLost = "afternoon";
                    break;
                case "EV":
                    formattedTimeLost = "evening";
                    break;
                default:
                    formattedTimeLost = Convert.ToDateTime(timeLost).ToString("h:mm tt");
                    break;
            }

            return (name, phoneNumber, dateLost, formattedDateLost, formattedTimeLost);
        }

        private static SimpleCard SimpleCard(string cardTitle, string cardContent)
        {
            var card = new SimpleCard
            {
                Title = cardTitle,
                Content = cardContent
            };
            return card;
        }

        private static async Task<SkillResponse> HandleReportLostItemIntent(Intent intent, SkillRequest input,
            ILambdaContext context)
        {
            var (name, phoneNumber, dateLostValue, _, formattedTimeLost) = FormatSlotValues(intent);
            var lostItem = new LostItem
            {
                Name = name,
                PhoneNumber = $"{phoneNumber}",
                ItemDescription = intent.Slots["itemDescription"].Value,
                LocationLost = intent.Slots["locationLost"].Value,
                DateLost = Convert.ToDateTime(dateLostValue),
                TimeLost = $"Around {formattedTimeLost}",
                Status = "Missing"
            };
            var items = new List<ListItem>();
            var id = 1;
            PropertyInfo[] properties = lostItem.GetType().GetProperties();
            foreach (var pi in properties)
            {
                var item = new ListItem
                {
                    Token = $"item_{id}",
                    Content = new TemplateContent
                    {
                        Primary = new TemplateText
                        {
                            Text = Regex.Replace(pi.Name, "([A-Z])", " $1"),
                            Type = TextType.Plain
                        },
                        Secondary = new TemplateText
                        {
                            Text = FormatDateTimePropertyValue(pi, lostItem),
                            Type = TextType.Plain
                        }
                    }
                };
                items.Add(item);
                id++;
            }

            var actual = new ListTemplate1
            {
                Token = "list_template_one",
                BackgroundImage = new TemplateImage
                {
                    ContentDescription = "NYP SIT",
                    Sources = new List<ImageSource>
                    {
                        new ImageSource {Url = BackgroundImageSource}
                    }
                },
                Title = "Lost and Found Summary",
                Items = items
            };
            var progressive = new ProgressiveResponse(input);
            if (progressive.CanSend())
            {
                await progressive.SendSpeech("OK, please wait while I save the details of your lost item.");
            }

            if (progressive.CanSend())
            {
                Uri uri = await CreateLostItemAsync(lostItem, context);
                if (uri == null)
                {
                    return MakeSkillResponse(
                        "Sorry, I'm unable to save the details of your lost item. Please try again later.",
                        true);
                }

                var response =
                    ResponseBuilder.Tell(
                        "OK, I've saved the details of your lost item and I'll contact you once it has been found.");
                response.Response.Directives.Add(new DisplayRenderTemplateDirective {Template = actual});
                return response;
            }

            return null;
        }

        private static string FormatDateTimePropertyValue(PropertyInfo pi, LostItem lostItem)
        {
            var propertyValue = pi.PropertyType == typeof(DateTime)
                ? Convert.ToDateTime(pi.GetValue(lostItem)).ToString("MMM d, yyyy")
                : pi.GetValue(lostItem).ToString();
            return propertyValue;
        }

        private static async Task<Uri> CreateLostItemAsync(LostItem lostItem, ILambdaContext context)
        {
            HttpResponseMessage response = null;
            var uri = new Uri($"{Url}/api/LostItems");
            context.Logger.LogLine($"Attempting to post data to {uri.AbsoluteUri}");
            try
            {
                response = await _httpClient.PostAsJsonAsync(uri, lostItem);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                context.Logger.LogLine($"Exception: {e.Message}");
                context.Logger.LogLine($"Stack Trace: {e.StackTrace}");
            }

            // Return the URI of the created resource.
            return response?.Headers.Location;
        }

        private static async Task<Staff> GetStaffResultAsync(ILambdaContext context)
        {
            Staff staff = null;
            var uri = new Uri($"{Url}/api/Staff/GetStaffResult/L.315/");
            context.Logger.LogLine($"Attempting to fetch data from {uri.AbsoluteUri}");
            try
            {
                var response = await _httpClient.GetStringAsync(uri);
                context.Logger.LogLine($"Response from URL:\n{response}");
                // TODO: (PMO) Handle bad requests
                staff = JsonConvert.DeserializeObject<Staff>(response);
            }
            catch (Exception e)
            {
                context.Logger.LogLine($"Exception: {e.Message}");
                context.Logger.LogLine($"Stack Trace: {e.StackTrace}");
            }

            return staff;
        }

        private static BodyTemplate1 CreateBodyTemplate1(string title, string primaryText)
        {
            var actual = new BodyTemplate1
            {
                Token = "A2079",
                BackgroundImage = new TemplateImage
                {
                    ContentDescription = "NYP SIT",
                    Sources = new List<ImageSource>
                    {
                        new ImageSource {Url = BackgroundImageSource}
                    }
                },
                Title = title,
                Content = new TemplateContent
                {
                    Primary = new TemplateText
                    {
                        Text = $"<font size='5'>{primaryText}</font>",
                        Type = TextType.Rich
                    }
                }
            };
            return actual;
        }

        private static BodyTemplate7 CreateBodyTemplate7(string title, string contentDescription, string path)
        {
            var actual = new BodyTemplate7
            {
                Token = "SampleTemplate_3476",
                Title = title,
                BackgroundImage = new TemplateImage
                {
                    ContentDescription = "NYP SIT",
                    Sources = new List<ImageSource>
                    {
                        new ImageSource {Url = BackgroundImageSource}
                    }
                },
                Image = new TemplateImage
                {
                    ContentDescription = contentDescription,
                    Sources = new List<ImageSource>
                    {
                        new ImageSource
                        {
                            Url = Url + path
                        }
                    }
                }
            };
            return actual;
        }

        private static SkillResponse GetElementSelectedResponse(string token)
        {
            SkillResponse response;
            switch (token)
            {
                case "proc_item_0":
                case "1":
                {
                    const string primaryText =
                        "Log in to the Student Portal at http://myportal.nyp.edu.sg. " +
                        "Next, under the \"e-Services\" column on the left, hover over the \"Personal Information\" " +
                        "list item and click <b>Statement of Absence</b>.";
                    var actual = CreateBodyTemplate1("Log in to the Student Portal", primaryText);
                    response = ResponseBuilder.Tell(
                        "Log in to the Student Portal at myportal dot n y p dot e d u dot s g. " +
                        "Next, under the \"e-Services\" column on the left, hover over the \"Personal Information\" " +
                        "list item and click Statement of Absence.");
                    response.Response.Directives.Add(new DisplayRenderTemplateDirective {Template = actual});
                    response.Response.ShouldEndSession = false;
                    return response;
                }
                case "proc_item_1":
                case "2":
                {
                    var actual = CreateBodyTemplate7(
                        "Print out the SOA after online submission in the Student Portal", "Submit SOA",
                        "/assets/images/submit-soa.PNG");
                    response = ResponseBuilder.Tell(
                        "Next, fill out the details of your absence. Indicate your absence start and end date, " +
                        "and reason of absence. Click Retrieve Modules from Timetable or alternatively, " +
                        "click Add Other Modules to add the modules which you were absent for. " +
                        "Click Submit once you're done and print out the S.O.A.");
                    response.Response.Directives.Add(new DisplayRenderTemplateDirective {Template = actual});
                    response.Response.ShouldEndSession = false;
                    return response;
                }
                case "proc_item_2":
                case "3":
                {
                    const string primaryText =
                        "After printing out your SOA, attach your medical certificate " +
                        "onto the SOA printout if applicable.";
                    var actual =
                        CreateBodyTemplate1("Attach the original MC onto the SOA printout if applicable",
                            primaryText);
                    response = ResponseBuilder.Tell(
                        "After printing out your S.O.A., attach your medical certificate " +
                        "onto the S.O.A. printout if applicable.");
                    response.Response.Directives.Add(new DisplayRenderTemplateDirective {Template = actual});
                    response.Response.ShouldEndSession = false;
                    return response;
                }
                case "proc_item_3":
                case "4":
                {
                    const string primaryText =
                        "Lastly, submit your SOA and medical certificate/supporting documents to the " +
                        "submission box at the reception counter within two working days from your date of absence.";
                    var actual =
                        CreateBodyTemplate1("Submit the SOA and MC/supporting documents",
                            primaryText);
                    response = ResponseBuilder.Tell(
                        "Lastly, submit your S.O.A. and medical certificate/supporting documents to the " +
                        "submission box at the reception counter within two working days from your date of absence.");
                    response.Response.Directives.Add(new DisplayRenderTemplateDirective {Template = actual});
                    response.Response.ShouldEndSession = false;
                    return response;
                }
                default:
                    response = ResponseBuilder.Empty();
                    response.Response.ShouldEndSession = false;
                    return response;
            }
        }

        private static ListTemplate1 GetSubmitStatementOfAbsenceListTemplate1()
        {
            var items = new List<ListItem>();
            var process = new List<string[]>
            {
                new[]
                {
                    "Log in to the Student Portal", "Print out the SOA after online submission",
                    "Attach the original MC onto the SOA printout",
                    "Submit the SOA and MC/supporting documents"
                }
            };
            for (int i = 0; i < process[0].Length; i++)
            {
                var item = new ListItem
                {
                    Token = $"proc_item_{i}",
                    Content = new TemplateContent
                    {
                        Primary = new TemplateText
                        {
                            Text = $"<font size='5'>{process[0][i]}</font>",
                            Type = TextType.Rich
                        }
                    }
                };
                items.Add(item);
            }

            var actual = new ListTemplate1
            {
                Token = "list_template_one",
                BackButton = BackButtonVisibility.Hidden,
                BackgroundImage = new TemplateImage
                {
                    ContentDescription = "NYP SIT",
                    Sources = new List<ImageSource>
                    {
                        new ImageSource {Url = BackgroundImageSource}
                    }
                },
                Title = "Submit your statement of absence (SOA)",
                Items = items
            };
            return actual;
        }

        internal class Staff
        {
            public int Key { get; set; }

            public int Rank { get; set; }

            public string Name { get; set; }

            public string PhoneNumber { get; set; }

            public string Status { get; set; }

            public string EchoDeviceName { get; set; }
        }

        internal class DirectionsStep
        {
            public string StepInstructions { get; set; }

            public string StepAction { get; set; }

            public string StepActionImage { get; set; }

            public string ContentDescription { get; set; }
        }

        internal class LostItem
        {
            public string Name { get; set; }

            public string PhoneNumber { get; set; }

            public string ItemDescription { get; set; }

            public string LocationLost { get; set; }

            public DateTime DateLost { get; set; }

            public string TimeLost { get; set; }

            public string Status { get; set; }
        }
        #endregion
    }
}
