using System.Collections.Generic;
public class MessageClass
{
    public enum TYPEOFMESSAGE
    {
        Acknowledgment,
        Input,
        Connection,
        Disconnection,
        CharacterUpdate,
        WorldUpdate,
        MessagesNeeded
    }

    public enum INPUT
    {
        W,
        WIdle,
        A,
        AIdle,
        S,
        SIdle,
        D,
        DIdle,
        Attack,
        Idle
    }

    //public enum OBJECTUPDATE
    //{
    //    Appeared,
    //    Destroyed
    //}

    public uint id;
    public int playerID;
    public TYPEOFMESSAGE typeOfMessage;
    public INPUT input;
    public int objectID;
    public System.DateTime time;
    public Dictionary<int, List<uint>> messagesNeeded;
    public bool messagesLostInBetween;
    //public OBJECTUPDATE objectUpdate;

    public MessageClass(uint i, int pi, TYPEOFMESSAGE type, System.DateTime t)
    {
        id = i;
        playerID = pi;
        typeOfMessage = type;
        time=t;
    }

    public MessageClass(uint i, int pi, TYPEOFMESSAGE type, System.DateTime t, INPUT inp)
    {
        id = i;
        playerID = pi;
        typeOfMessage = type;
        time = t;
        input = inp;
    }

    public MessageClass(uint i, int pi, TYPEOFMESSAGE type, System.DateTime t, bool lost)
    {
        id = i;
        playerID = pi;
        typeOfMessage = type;
        time = t;
        messagesLostInBetween = lost;
    }

    public MessageClass(uint i, int pi, TYPEOFMESSAGE type, System.DateTime t, int oi/*, OBJECTUPDATE ou*/)
    {
        id = i;
        playerID = pi;
        typeOfMessage = type;
        time = t;
        objectID = oi;
        //objectUpdate = ou;
    }

    public MessageClass(uint i, int pi, TYPEOFMESSAGE type, System.DateTime t, Dictionary<int, List<uint>> needed)
    {
        id = i;
        playerID = pi;
        typeOfMessage = type;
        time = t;
        messagesNeeded = needed;
    }

    public MessageClass(string str)
    {
        string[] info= str.Split('#');
        id = uint.Parse(info[0]);
        playerID = int.Parse(info[1]);
        typeOfMessage = (TYPEOFMESSAGE)int.Parse(info[2]);
        System.DateTime.Parse(info[3]);
        switch (typeOfMessage)
        {
            case TYPEOFMESSAGE.Input:
                input = (INPUT)int.Parse(info[4]);
                break;
            case TYPEOFMESSAGE.WorldUpdate:
                objectID = int.Parse(info[4]);
                break;
            case TYPEOFMESSAGE.Acknowledgment:
                messagesLostInBetween = bool.Parse(info[4]);
                break;
            case TYPEOFMESSAGE.MessagesNeeded:
                string[] numbers = info[4].Split(';');
                string[] specificNumbers;
                messagesNeeded = new Dictionary<int, List<uint>>();
                for(int i = 0; i < numbers.Length; i++)
                {
                    specificNumbers = numbers[i].Split(',');
                    List<uint> ids = new List<uint>();
                    for(int j = 1;  j < specificNumbers.Length; j++)
                    {
                        ids.Add(uint.Parse(specificNumbers[j]));
                    }
                    messagesNeeded.Add(int.Parse(specificNumbers[0]), ids);
                }
                break;
            default:
                break;
        }
    }

    public string Serialize()
    {
        string info;
        switch (typeOfMessage)
        {
            case TYPEOFMESSAGE.Input:
                info = '#' + input.ToString("d");
                break;
            case TYPEOFMESSAGE.WorldUpdate:
                info = '#' + objectID.ToString();
                break;
            case TYPEOFMESSAGE.Acknowledgment:
                info = '#' + messagesLostInBetween.ToString();
                break;
            case TYPEOFMESSAGE.MessagesNeeded:
                bool firstNumber = true;
                string numbers="";
                foreach(var number in messagesNeeded)
                {
                    if (!firstNumber)
                    {
                        numbers += ';';
                    }
                    numbers += number.Key;
                    numbers += ',';
                    foreach(var ids in number.Value)
                    {
                        numbers += ids;
                    }
                    numbers += number.Key;
                    firstNumber = false;
                }
                info = '#' + numbers;
                break;
            default:
                info = "";
                break;
        }
        return id.ToString() + '#' + playerID.ToString() + '#' + typeOfMessage.ToString("d") + '#' + time.ToString() + info;
    }

    public static List<MessageClass> CheckIfThereAreMessagesLost(ref Dictionary<int, uint> listOfMessages, ref Dictionary<int, List<uint>> fullListOfMessagesLost, MessageClass message, int index)
    {
        uint lastMessageID;
        List<uint> listOfMessagesLost = fullListOfMessagesLost[index];
        uint idMessage = message.id;
        bool thereAreMessagesLost = false;
        List<MessageClass> messagesToSend = new List<MessageClass>();


        if (!listOfMessages.ContainsKey(index))
        {
            listOfMessages.Add(index, idMessage);

            if (idMessage > 0)
            {
                for (uint i = 0; i < idMessage; i++)
                {
                    listOfMessagesLost.Add(i);
                }
                fullListOfMessagesLost[index] = listOfMessagesLost;
            }
        }
        else
        {
            lastMessageID = listOfMessages[index];
            if (idMessage < lastMessageID)
            {
                listOfMessagesLost.Remove(idMessage);
                fullListOfMessagesLost[index] = listOfMessagesLost;
            }
            else
            {
                listOfMessages[index] = idMessage;
                for(uint i = lastMessageID + 1; i < idMessage; i++)
                {
                    listOfMessagesLost.Add(i);
                }
                fullListOfMessagesLost[index] = listOfMessagesLost;
            }
        }

        if (fullListOfMessagesLost.Count > 0)
        {
            thereAreMessagesLost = true;
        }
        

        

        MessageClass messageToSend;
        if (thereAreMessagesLost)
        {
            messageToSend = new MessageClass(idMessage, message.playerID, MessageClass.TYPEOFMESSAGE.MessagesNeeded, System.DateTime.Now, fullListOfMessagesLost);
            for (int i = 0; i < 3; i++)
            {
                messagesToSend.Add(messageToSend);
            }
        }
        messageToSend = new MessageClass(idMessage, message.playerID, MessageClass.TYPEOFMESSAGE.Acknowledgment, System.DateTime.Now, thereAreMessagesLost);
        messagesToSend.Add(messageToSend);

        return messagesToSend;
    }
}
