namespace SF.CodeGeneration;

public static partial class CSharpFile
{
    public static void DeclareMethod(MethodDeclaration method)
    {
        MethodXmlComment(method);

        if(method.Attributes?.Count > 0)
        {
            foreach(var attribute in method.Attributes)
                _b.Append(_currentIndent).Append('[').Append(attribute).Append(']').AppendLine();
        }

        MemberSignature(method);

        ComposeGenericParams(method);

        _b.Append('(');
        if(method.Parameters?.Count > 0)
        {
            var count = method.Parameters.Count;
            for(var i = 0; i < count; i++)
            {
                var p = method.Parameters[i];
                _b.Append(p.ParamType).Append(' ').Append(p.Name);

                if(!string.IsNullOrWhiteSpace(p.DefaultValue))
                    _b.Append(' ').Append('=').Append(' ').Append(p.DefaultValue);

                _b.Append(',').Append(' ');
            }

            _b.Length--;
            _b.Length--;
        }

        _b.Append(')');

        ComposeGenericConstraints(method);

        if(method.IsInterfaceMember || method.InheritanceModifier == InheritanceModifiers.Abstract || method.IsPartial)
        {
            _b.Append(';');
        }

        _b.AppendLine();
    }
}
