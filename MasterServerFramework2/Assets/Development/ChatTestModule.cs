using System.Collections;
using System.Collections.Generic;
using Barebones.MasterServer;
using UnityEngine;

public class ChatTestModule : ServerModuleBehaviour
{
    private ChatModule _module;

    void Awake()
    {
        AddDependency<ChatModule>();
    }

    public override void Initialize(IServer server)
    {
        base.Initialize(server);

        _module = server.GetModule<ChatModule>();

        server.SetHandler(777, message =>
        {
            var username = message.AsString();
            Logs.Debug("Changing username to: " + username);

            _module.ChangeUsername(message.Peer, username);

        });
    }

}
