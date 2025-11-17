---
title: CI
---

# Continuous Integration

:::note
GitHub Actions are primarily used to trigger our own internal FAKE build system, so this
should hopefully be easy to manage.
:::

## Docs

A marker system is implemented to capture regions of source code.
The markers are of the form `//%<MARKER_NAME>%<START|END>%`.

As an example:
```fsharp
let say = null
//%TEST%START%
let hello = "world"
//%TEST%END%
let bye = "world"
```

And in MDX:
```jsx
import raw from '!!raw-loader!../path/to/source/code.fs';
import GetRegion from '@site/src/components/LineRegions';
import CodeBlock from '@theme/CodeBlock';

<CodeBlock language={'fsharp'}>{GetRegion(raw, 'TEST')}</CodeBlock>
```

Would output:

```fsharp
let hello = "world"
```

:::note
Other region marker lines are erased from the content of a different region marker.
:::
