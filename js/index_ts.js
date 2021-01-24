const fs = require('fs');
const ts = require("typescript");

fs.readFile(__filename, 'utf8', function(err, data) {
    if (err) throw err;
    console.log(`Successfully fetched data from ${__filename}.`);
    // now fetch tokens
    const sourceFile = ts.createSourceFile(
        __filename, data, ts.ScriptTarget.Latest
    );
    const collector = new Array();
    const runningIndexWrapper = { i: 0 };
    retrieveTerminalChildren(sourceFile, sourceFile, collector, runningIndexWrapper);
    for (const token of collector) {
        console.log(token);
    }
});

function retrieveTerminalChildren(sourceFile, node, collector, runningIndexWrapper) {
    if (node.getChildCount(sourceFile)) {
        node.getChildren(sourceFile).forEach(c => retrieveTerminalChildren(sourceFile, c, collector, runningIndexWrapper));
    }
    else {
        const rawType = ts.SyntaxKind[node.kind];
        const isKeyword = rawType.endsWith("Keyword");
        const genericType = getGenericType(rawType, isKeyword);
        const text = node.getText(sourceFile);
        const startPos = node.getStart(sourceFile);
        const endPos = node.getEnd(sourceFile);
        const lineNumber = sourceFile.getLineAndCharacterOfPosition(startPos).line + 1;
        
        const leadingComments = ts.getLeadingCommentRanges(sourceFile.getText(), node.getFullStart(sourceFile)) || [];
        for (const comment of leadingComments) {
            const commentToken = {
                index: runningIndexWrapper.i++,
                lineNumber: sourceFile.getLineAndCharacterOfPosition(comment.pos).line + 1,
                genericType: "comment",
                rawType: ts.SyntaxKind[comment.kind],
                isKeyword: false,
                startPos: comment.pos,
                endPos: comment.end,
                text: sourceFile.getText().substring(comment.pos, comment.end),
                isTrivia: true
            };
            collector.push(commentToken);
        }
        
        const myToken = {
            index: runningIndexWrapper.i++,
            lineNumber,
            rawType,
            genericType,
            isKeyword,
            startPos,
            endPos,
            text,
            isTrivia: false
        };
        collector.push(myToken);
        
        const trailingComments = ts.getTrailingCommentRanges(sourceFile.getText(), endPos) || [];
        for (const commment of trailingComments) {
            const commentToken = {
                index: runningIndexWrapper.i++,
                lineNumber: sourceFile.getLineAndCharacterOfPosition(comment.pos).line + 1,
                genericType: "comment",
                rawType: ts.SyntaxKind[comment.kind],
                isKeyword: false,
                startPos: comment.pos,
                endPos: comment.end,
                text: sourceFile.getText().substring(comment.pos, comment.end),
                isTrivia: true
            };
            collector.push(commentToken);
        }
    }
}

function getGenericType(rawTokenType, isKeyword) {
    if (isKeyword) {
        return 'keyword';
    }
    switch (rawTokenType) {
        case 'Identifier':
            return 'identifier';
        case 'StringLiteral':
            return 'string';
        case 'EndOfFileToken':
            return 'eof';
        default:
            return null;
    }
}