using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Xunit;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;

using SkillMazowieckieRailways;

namespace SkillMazowieckieRailways.Tests
{
    
    public class FunctionTest
    {
        [Fact]
        public void TestToUpperFunction()
        {


            var request = new Alexa.NET.Request.SkillRequest();
            request.Request 


            var function = new Function();
            var context = new TestLambdaContext();
            var response = function.FunctionHandler(request, context).Result;

            
        }
    }
}
