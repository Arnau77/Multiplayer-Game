using System.Collections.Generic;
public class MessageClass
{
    public enum TYPEOFMESSAGE
    {
        Acknoledgment,
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
    public Dictionary<uint, int> messagesNeeded;
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

    public MessageClass(uint i, int pi, TYPEOFMESSAGE type, System.DateTime t, int oi/*, OBJECTUPDATE ou*/)
    {
        id = i;
        playerID = pi;
        typeOfMessage = type;
        time = t;
        objectID = oi;
        //objectUpdate = ou;
    }

    public MessageClass(uint i, int pi, TYPEOFMESSAGE type, System.DateTime t, Dictionary<uint, int> needed)
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
            case TYPEOFMESSAGE.MessagesNeeded:
                string[] numbers = info[4].Split(';');
                string[] specificNumbers;
                messagesNeeded = new Dictionary<uint, int>();
                for(int i = 0; i < numbers.Length; i++)
                {
                    specificNumbers = numbers[i].Split(',');
                    messagesNeeded.Add(uint.Parse(specificNumbers[1]), int.Parse(specificNumbers[0]));
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
            case TYPEOFMESSAGE.MessagesNeeded:
                bool firstNumber = true;
                string numbers="";
                foreach(var number in messagesNeeded)
                {
                    if (!firstNumber)
                    {
                        numbers += ';';
                    }
                    numbers += number.Value;
                    numbers += ',';
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
}
