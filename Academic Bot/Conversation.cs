using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

namespace Academic_Bot
{
    [Serializable]
    public class ConversationDialog : IDialog
    {
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }
        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<Message> argument)
        {
            var message = await argument;
            string name = string.Empty;
            name = message.GetBotPerUserInConversationData<string>("name");
            if (string.IsNullOrEmpty(name))
            {
                PromptDialog.Text(context, AfterResetAsync, "Hi! What is your name?");
            }
            else
            {
                bool welcome = false;
                context.PerUserInConversationData.TryGetValue<bool>("welcome", out welcome);

                if (!welcome)
                {
                    await context.PostAsync($"Welcome back {name}");
                    context.PerUserInConversationData.SetValue<bool>("welcome", true);
                    context.Wait(MessageReceivedAsync);
                }
                else if ((message.Text.ToLower().Equals("hi")) || (message.Text.ToLower().Equals("hello")))
                {
                    await context.PostAsync("Hello again!");
                    context.Wait(MessageReceivedAsync);
                }
                else
                {
                    //Interpret query
                    AcademicResult resp = await Utilities.Interpret(message.Text.ToLower());
                    if (resp.interpretations.Count == 0)
                    {
                        resp = await Utilities.Interpret(message.Text.ToLower());
                    }

                    //TODO: check resp
                    XmlDocument doc = new XmlDocument();
                    string xPath = "rule/attr";
                    string responseMessage = string.Empty;
                    string parse = string.Empty;

                    if (resp.interpretations.Count == 0)
                    {
                        await context.PostAsync("Sorry i couldn't find anything. Please try again.");
                        context.Wait(MessageReceivedAsync);
                    }
                    else
                    {
                        context.PerUserInConversationData.SetValue<AcademicResult>("result", resp);
                        int counter = 1;
                        List<int> options = new List<int>();

                        //Get proper text for each interpretations
                        foreach (var interp in resp.interpretations)
                        {
                            //Add to options
                            options.Add(counter);

                            doc.LoadXml(interp.parse);
                            var nodes = doc.SelectNodes(xPath);
                            parse = string.Empty;
                            List<Item> attributes = new List<Item>();


                            foreach (XmlNode node in nodes)
                            {
                                if (node.Attributes.Count == 2)
                                {
                                    if (node.Attributes[1].Name == "canonical")
                                    {
                                        attributes.Add(new Item { Attribute = node.Attributes[0].Value, Value = node.Attributes[1].Value });
                                    }
                                    else
                                    {
                                        attributes.Add(new Item { Attribute = node.Attributes[0].Value, Value = node.Attributes[1].Value });
                                    }
                                }
                                else
                                {
                                    attributes.Add(new Item { Attribute = node.Attributes[0].Value, Value = node.InnerText });
                                }
                            }

                            parse = "Papers " + ProcessAttribute(attributes);
                            responseMessage += $"- {counter} : {parse} \r\n";
                            counter += 1;

                        }

                        options.Add(counter);
                        responseMessage += $"- {counter} : Search something else \r\n";

                        //Post reply
                        responseMessage = "Here is what I found. Simply reply with the number of your choice \r\n" + responseMessage;
                        PromptDialog.Number(context, AfterChosenAsync, responseMessage,"Sorry! I did not understand. Please choose from above options.");
                    }
                }
            }
        }

        private string ProcessAttribute(List<Item> attributes)
        {
            var atts = attributes.GroupBy(g => g.Attribute).Select(grp => grp.ToList()).ToList(); ; //  .OrderBy(x => x.Attribute).ToList<Item>();
            string attributeTitle = string.Empty;
            string Title = string.Empty;

            foreach (var item in atts)
            {
                string attributeName = item[0].Attribute;

                switch (attributeName)
                {
                    case "academic#F.FN":
                        Title = "in field ";
                        break;
                    case "academic#Ti":
                        Title = "with title ";
                        break;
                    case "academic#Y":
                        Title = "in year ";
                        break;
                    case "academic#D":
                        Title = "date ";
                        break;
                    case "academic#CC":
                        Title = "with citation count ";
                        break;
                    case "academic#AA.AuN":
                        Title = "by author ";
                        break;
                    case "academic#AA.AuId":
                        Title = "with author id ";
                        break;
                    case "academic#AA.AfN":
                        Title = "with author affiliation ";
                        break;
                    case "academic#AA.AfId":
                        Title = "with affiliation id ";
                        break;
                    case "academic#F.FId":
                        Title = "with field id ";
                        break;
                    case "academic#J.JN":
                        Title = "with journal name ";
                        break;
                    case "academic#J.Id":
                        Title = "with journal id ";
                        break;
                    case "academic#C.CN":
                        Title = "with conference name ";
                        break;
                    case "academic#C.Id":
                        Title = "with conference id ";
                        break;
                    case "academic#RId":
                        Title = "with reference id ";
                        break;
                    case "academic#W":
                        Title = "with words ";
                        break;
                    case "academic#E":
                        Title = "";
                        break;
                    default:
                        Title = "";
                        break;
                }

                attributeTitle += Title;

                int counter = 0;
                foreach (var i in item)
                {
                    if (counter == 0)
                    {
                        attributeTitle += $"**{i.Value}** ";
                    }
                    else if (counter == item.Count - 1)
                    {
                        attributeTitle += $"and **{i.Value}** ";
                    }
                    else
                    {
                        attributeTitle += $",**{i.Value}** ";
                    }
                    counter++;

                }
            }

            return attributeTitle;

        }

        public async Task AfterResetAsync(IDialogContext context, IAwaitable<string> argument)
        {
            var name = await argument;
            context.PerUserInConversationData.SetValue<string>("name", name);
            context.PerUserInConversationData.SetValue<bool>("welcome", true);
            await context.PostAsync($"Hi! {name}, I am Academic Knowledge guru. You can simply type in the title to search or search by Authors by replying with \"papers by AUTHOR NAME\".");
            context.Wait(MessageReceivedAsync);
        }

        public async Task AfterChosenAsync(IDialogContext context, IAwaitable<long> argument)
        {
            var choice = await argument;
            AcademicResult result = new AcademicResult();
            context.PerUserInConversationData.TryGetValue<AcademicResult>("result", out result);
            string responseMessage = string.Empty;
            string linkMsg = string.Empty;

            if (result != null)
            {
                if (choice > result.interpretations.Count())
                {
                    await context.PostAsync("Okay! ask me.");
                }
                else
                {
                    EvaluateResult resp = await Utilities.Evaluate(result.interpretations[Convert.ToInt16(choice) - 1].rules[0].output.value);
                    int counter = 1;
                    foreach (var en in resp.entities)
                    {
                        EX ex = JsonConvert.DeserializeObject<EX>(en.E);
                        linkMsg = string.Empty;
                        foreach (var link in ex.S)
                        {
                            switch (link.Ty)
                            {
                                case 1:
                                    linkMsg += $" [HTML]({link.U})";
                                    break;
                                case 2:
                                    linkMsg += $" [TEXT]({link.U})";
                                    break;
                                case 3:
                                    linkMsg += $" [PDF]({link.U})";
                                    break;
                                case 4:
                                    linkMsg += $" [DOC]({link.U})";
                                    break;
                                case 5:
                                    linkMsg += $" [PPT]({link.U})";
                                    break;
                                case 6:
                                    linkMsg += $" [XLS]({link.U})";
                                    break;
                                case 7:
                                    linkMsg += $" [PS]({link.U})";
                                    break;
                                default:
                                    linkMsg += $" [LINK]({link.U})";
                                    break;
                            }

                        }

                        responseMessage += $"- {counter} . {ex.DN} {linkMsg} \r\n";
                        counter++;
                    }

                    await context.PostAsync(responseMessage);
                }
            }

            context.Wait(MessageReceivedAsync);
        }
    }
}

