using Grapevine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MelBoxWeb
{

    //[RestResource]
    //public partial class Routen
    //{
        //[RestRoute("Get", "/set/{name}/{value}")]
        //public async Task SetCookie(IHttpContext context)
        //{
        //    var name = context.Request.PathParameters["name"];
        //    var value = context.Request.PathParameters["value"];

        //    context.Response.Cookies.Add(new System.Net.Cookie(name, value, "/"));
        //    await context.Response.SendResponseAsync(HttpStatusCode.Ok);
        //}

        //[RestRoute("Get", "/get/{name}")]
        //public async Task GetCookie(IHttpContext context)
        //{
        //    var name = context.Request.PathParameters["name"];
        //    var cookie = context.Request.Cookies.Where(c => c.Name == name).FirstOrDefault();
        //    await context.Response.SendResponseAsync($"Cookie Value: {cookie.Value}");
        //}


    //}
}
