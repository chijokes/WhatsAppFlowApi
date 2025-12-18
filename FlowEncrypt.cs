using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WhatsAppFlowApi;

public sealed record FlowEncryptedRequest(
    string encrypted_flow_data,
    string encrypted_aes_key,
    string initial_vector
);


