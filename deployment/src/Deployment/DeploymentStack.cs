using Amazon.CDK;
using Amazon.CDK.AWS.Events;
using Amazon.CDK.AWS.Events.Targets;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using System.Collections.Generic;

namespace Deployment
{
    public class DeploymentStack : Stack
    {
        internal DeploymentStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            // role
            var role = new Role(this, "fixsecuritygroupslambdarole", new RoleProps
            {
                Description = "fix security groups lambda role",
                AssumedBy = new ServicePrincipal("lambda.amazonaws.com")
            });
            role.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("AmazonEC2FullAccess"));
            role.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("AWSLambdaExecute"));
            role.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("CloudWatchEventsFullAccess"));

            // lambda
            var code = @"
import json
import boto3

def lambda_handler(event, context):
    print(event)
    if event['detail']['eventSource'] == 'ec2.amazonaws.com' and event['detail']['eventName'] == 'CreateSecurityGroup':
        sgid = event['detail']['responseElements']['groupId']
    elif event['detail']['eventSource'] == 'ec2.amazonaws.com' and event['detail']['eventName'] == 'DeleteTags':
        sgid = event['detail']['requestParameters']['resourcesSet']['items'][0]['resourceId']
    ec2 = boto3.resource('ec2')
    sg = ec2.SecurityGroup(sgid)
    sg.create_tags(Tags=[{'Key': 'Name', 'Value': ""TAGGED!!!""}])
    return {
        'statusCode': 200,
        'body': json.dumps(event)
    }
";
            var lambda = new Function(this, "fixsecuritygroupslambda", new FunctionProps
            {
                Runtime = Runtime.PYTHON_3_6,
                Code = Code.FromInline(code),
                Handler = "index.lambda_handler",
                Role = role
            });

            // cloudwatch event
            //       @"{
            //          ""source"": [
            //            ""aws.ec2""
            //          ],
            //          ""detail-type"": [
            //            ""AWS API Call via CloudTrail""
            //          ],
            //          ""detail"": {
            //            ""eventSource"": [
            //              ""ec2.amazonaws.com""
            //            ],
            //            ""eventName"": [
            //              ""CreateSecurityGroup"",
            //              ""DeleteTags""
            //            ]
            //          }
            //        }";
            var ed = new Dictionary<string, object>();
            ed.Add("eventSource", new string[1] { "ec2.amazonaws.com" });
            ed.Add("eventName", new string[2] { "CreateSecurityGroup", "DeleteTags" });
            var rule = new Rule(this, "cloudwatchevent", new RuleProps
            {
                Enabled = true,
                EventPattern = new EventPattern()
                {
                    Detail = ed,
                    Source = new string[1] { "aws.ec2" },
                    DetailType = new string[1] { "AWS API Call via CloudTrail" }
                },
            });
            rule.AddTarget(new LambdaFunction(lambda));
        }
    }
}
