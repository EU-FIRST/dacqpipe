<%@ Page Language="C#" %>

<%  
    string docId = Request.Params["docId"];
    string corpusId = Request.Params["corpusId"];
    string format = Request.Params["format"];
    bool rmvRaw = (Request.Params["rmvRaw"] != null) ? new Regex(@"(true)|(yes)|(on)|(1)|(t)|(y)", RegexOptions.IgnoreCase).Match(Request.Params["rmvRaw"]).Success : false;
    if (format == "txt")
    {
        Response.ContentType = "text/plain; charset=utf-8";
    }
    else if (format == "html")
    {
        Response.ContentType = "text/html; charset=utf-8";
    }
    else // gate_xml, xml
    {
        Response.ContentType = "application/xml; charset=utf-8";
    }
    Response.Write(new Service().GetDoc(corpusId, docId, format, rmvRaw));
%>