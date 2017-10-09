# ServiceFabric.Dictionary
Demo of Service Fabric Stateful service with Stateless service acting as reverse proxy, 
which routes traffic to proper Stateful service replica.


## Azure API management

Use the walkthrough to configure Azure API Management to talk directly to the Stateful (Dictionary) service.

Apply this policy:

``` xml
<policies>
    <inbound>
        <base />
        <set-backend-service sf-partition-key="@{
            ulong FnvPrime = 1099511628211;
			const ulong FnvOffsetBasis = 14695981039346656037;
			var data = Encoding.UTF8.GetBytes(context.Request.MatchedParameters["word"].ToUpperInvariant());
			var hash = FnvOffsetBasis;
			for (int i = 0; i < data.Length; ++i)
			{
				hash ^= data[i];
				hash *= FnvPrime;
			}
			long longHash = (long)hash;
			return longHash;
        }" backend-id="servicefabric" sf-service-instance-name="fabric:/ServiceFabric.Dictionary/ServiceFabric.Dictionary.DictionaryService" sf-resolve-condition="@((int)context.Response.StatusCode==200)" />
    </inbound>
    <backend>
        <base />
    </backend>
    <outbound>
        <base />
    </outbound>
    <on-error>
        <base />
    </on-error>
</policies>

```

It uses the same hash algorithm as the IndexService and IndexApiService do, so the end-result is the same.
