using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FamtChatLibrary
{    
    public enum MessageType
    {
        ACK                 =   10,
        IDENTIFY            =   20,
        SUCCESS_IDENTIFY    =   30,
        FAILED_IDENTIFY     =   40,
        DUP_IDENTIFY        =   50,
        REQ_LIST            =   60,
        REC_LIST            =   70,
        REM_LIST            =   80,
        MSG_INIT            =   90,
        MSG                 =   100,
        DISCONNECT          =   110,
        LOGGEDIN            =   120,
        LOGGEDOUT           =   130
    }
}
