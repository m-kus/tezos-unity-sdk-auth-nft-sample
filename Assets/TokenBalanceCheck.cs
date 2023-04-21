using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text.Json;
using TezosAPI;

public static class JsonHelper 
{
    public static T[] FromJson<T>(string json_array) {
        string json_obj = "{\"items\":"+ json_array +"}";
        Wrapper<T> wrapper = UnityEngine.JsonUtility.FromJson<Wrapper<T>>(json_obj);
        return wrapper.items;
    }

    [Serializable]
    private class Wrapper<T> {
        public T[] items;
    }
}

[Serializable]
public class TokenBalance
{
    /// <summary>
    /// Internal TzKT id.  
    /// **[sortable]**
    /// </summary>
    public long id;

    /// <summary>
    /// Owner account.  
    /// Click on the field to expand more details.
    /// </summary>
    public string owner;

    /// <summary>
    /// Balance (raw value, not divided by `decimals`).  
    /// **[sortable]**
    /// </summary>
    public string balance;

    /// <summary>
    /// Contract, created the token.
    /// </summary>
    public string tokenContract;

    /// <summary>
    /// Token id, unique within the contract.
    /// </summary>
    public string tokenId;

    /// <summary>
    /// Token metadata.
    /// </summary>
    public JsonElement tokenMetadata;

    /// <summary>
    /// Timestamp of the block where the token balance was last changed.
    /// </summary>
    public string lastTime;  // JsonUtility is bad at deserializing this to DateTime
}

public class TokenBalanceCheck : MonoBehaviour
{
    private const string BaseUrl = "https://api.tzkt.io/v1/tokens/balances?balance.ne=0";

    private ITezosAPI _tezos;

    private void Start()
    {
        Debug.Log("Token balance check demo");
        _tezos = TezosSingleton.Instance;
        _tezos.MessageReceiver.AccountConnected += OnAccountConnected;
    }
    
    public void OnAccountConnected(string result)
    {
        string activeAddress = _tezos.GetActiveWalletAddress();
        Debug.Log("Active account: " + activeAddress);

        IsOwnerOfToken(activeAddress, "KT1BRADdqGk2eLmMqvyWzqVmPQ1RCBCbW5dY", 123);
        IsOwnerOfToken("tz1TiZ74DtsT74VyWfbAuSis5KcncH1WvNB9", "KT1BRADdqGk2eLmMqvyWzqVmPQ1RCBCbW5dY", 1);
        GetTokensForOwner("tz2U7C8cf4W5Qw6onYjF8QLhnh5hMRbrrDon");
    }

    public void IsOwnerOfToken(string account, string contract, int tokenId)
    {
        StartCoroutine(CheckTokenBalance(account, contract, tokenId));
    }

    public void GetTokensForOwner(string account)
    {
        StartCoroutine(GetTokenBalances(account));
    }

    private IEnumerator CheckTokenBalance(string account, string contract, int tokenId)
    {
        string url = $"{BaseUrl}&account={account}&token.contract={contract}&token.tokenId={tokenId}&select=id";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.DataProcessingError)
            {
                Debug.LogError("Error: " + request.error);
            }
            else
            {
                Debug.Log("Response: " + request.downloadHandler.text);
                bool isOwner = JsonHelper.FromJson<int>(request.downloadHandler.text).Length > 0;
                Debug.Log($"Account {account} ownership status for token {contract}#{tokenId}: " + isOwner);
            }
        }
    }

    private IEnumerator GetTokenBalances(string account)
    {
        string url = $"{BaseUrl}&account={account}&select=account.address%20as%20owner,balance,token.contract.address%20as%20tokenContract,token.tokenId%20as%20tokenId,token.metadata%20as%20tokenMetadata,lastTime,id";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.DataProcessingError)
            {
                Debug.LogError("Error: " + request.error);
            }
            else
            {
                Debug.Log("Response: " + request.downloadHandler.text);
                TokenBalance[] balances = JsonHelper.FromJson<TokenBalance>(request.downloadHandler.text);
                foreach (var balance in balances)
                {
                    Debug.Log($"{balance.tokenContract}#{balance.tokenId} => {balance.balance} (last updated {balance.lastTime})");
                }
            }
        }
    }
}

