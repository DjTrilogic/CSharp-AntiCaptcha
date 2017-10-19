# CSharp-AntiCaptcha
C# AntiCaptcha library


```
//Change software id
AntiCaptchaGlobals.SoftId = 3432;

//Change solution use limit
AntiCaptchaGlobals.CaptchaRetryLimit = 10;

//Add a key, add as many as you'd like the load balancer will roundrobin between keys
AntiCaptchaLoadBalancer.AddKey(new AntiCaptchaKey("your anti captcha key"));
AntiCaptchaLoadBalancer.AddKey(new AntiCaptchaKey("your anti captcha key"));

//Get a valid key from the pool
var key = new AntiCaptchaKey("your anti captcha key");

//View balance
var keyBalance = key.AntiCaptchaBalance;

//View queue statistics
AntiCaptchaGlobals.SelectedQueueStats = QueueIdEnum.RecaptchaProxyless;
var statistics = AntiCaptchaGlobals.GetStatsForSelectedQueue();

//Solve with key
var solutionUsingKey = await key.GetSolvedCaptcha(new NoCaptchaTaskProxyless("https://someurl.com", "google public key"));

//Solve with load balancer, this will reuse keys that were added back into the queue
var solutionUsingLoadBalancer = await AntiCaptchaLoadBalancer.GetSolvedCaptcha(new NoCaptchaTaskProxyless("https://someurl.com", "google public key"));

//Requeue a captcha that wasn't used
AntiCaptchaLoadBalancer.EnqueueResponse(solutionUsingLoadBalancer);```