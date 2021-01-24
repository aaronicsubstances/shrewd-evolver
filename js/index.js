const fs = require('fs');
const acorn = require("acorn");

fs.readFile(__filename, 'utf8', function(err, data) {
    if (err) throw err;
    console.log(`Successfully fetched data from ${__filename}.`);
    // now fetch tokens
    let runningIndex = 0;
    acorn.parse(data, {
        ecmaVersion: 2020,
        locations: true,
        onToken: function(token) {
            const rawTokenType = getRawTokenType(token.type);
            const isKeyword = !!token.type.keyword;
            const genericType = getGenericType(rawTokenType, isKeyword);
            let text = token.value;
            if (typeof text !== 'string') {
                text = data.substring(token.start, token.end);
            }
            console.log({
                index: runningIndex++,
                lineNumber: token.loc.start.line,
                rawType: rawTokenType,
                genericType,
                isKeyword,
                startPos: token.start,
                endPos: token.end,
                text,
                isTrivia: false
            });
        },
        onComment: function(block, text, startPos, endPos, startLoc, endLoc) {
            const rawType = `__${block ? 'block' : 'line'}_comment`;
            const genericType = getGenericType(rawType, false);
            console.log({
                index: runningIndex++,
                lineNumber: startLoc.line,
                rawType,
                genericType,
                isKeyword: false,
                startPos,
                endPos,
                text,
                isTrivia: true
            });
        }
    })
});

function getRawTokenType(tokenType) {
    for (const key of Object.getOwnPropertyNames(acorn.tokTypes)) {
        if (acorn.tokTypes[key] === tokenType) {
            return key;
        }
    }
}

function getGenericType(rawTokenType, isKeyword) {
    if (isKeyword) {
        return 'keyword';
    }
    switch (rawTokenType) {
        case 'name':
            return 'identifier';
        case 'num':
            return 'number';
        case 'string':
        case 'template':
            return 'string';
        case '__line_comment':
        case '__block_comment':
            return 'comment';
        case 'eof':
            return 'eof';
        default:
            return null;
    }
}