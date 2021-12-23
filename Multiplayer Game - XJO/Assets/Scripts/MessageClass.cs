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
        Attack
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
    int[,] messagesNeeded;
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

    public MessageClass(uint i, int pi, TYPEOFMESSAGE type, System.DateTime t, int[,] needed)
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
                messagesNeeded = new int[numbers.Length,2];
                for(int i = 0; i < numbers.Length; i++)
                {
                    specificNumbers = numbers[i].Split(',');
                    messagesNeeded[i, 0] = int.Parse(specificNumbers[0]);
                    messagesNeeded[i, 1] = int.Parse(specificNumbers[1]);
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
                string numbers="";
                for(int i = 0; i < messagesNeeded.Length/2; i++)
                {
                    if (i > 0)
                    {
                        numbers += ';';
                    }
                    numbers += messagesNeeded[i, 0];
                    numbers += ',';
                    numbers+= messagesNeeded[i, 1];
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
