import clsx from 'clsx';
import Heading from '@theme/Heading';
import styles from './styles.module.css';

const FeatureList = [
  {
    title: 'Generated from Source',
    Svg: require('@site/static/img/generated.png').default,
    description: (
      <>
          <code>Fable.Electron</code> is generated from the same source material
          as the <code>electron.d.ts</code> file.
      </>
    ),
  },
  {
    title: 'More than Type Safety',
    // Svg: require('@site/static/img/electron_os_win.png').default,
    Svg: require('@site/static/img/electron_os_win.png').default,
    description: (
      <>
          Extra methods and helpers such as <code>StringEnums</code>,
          named event methods, and conditional compilation for targetting
          a specific OS; all out of the box.
      </>
    ),
  },
  {
    title: 'Fable.Electron.Remoting',
    Svg: require('@site/static/img/fable_remoting.png').default,
    description: (
      <>
          Context-isolated IPC two-way and main -> renderer 
          in <code>Fable.Remoting</code> style out of the box! 
      </>
    ),
  },
];

function Feature({Svg, title, description}) {
  return (
    <div className={clsx('col col--4')}>
      <div className="text--center">
          {/*{title !== 'Generated from Source' ? (<img className={styles.featureImg} alt={'Feature image'} src={Svg}/>) : (<Svg className={styles.featureSvg} role={'img'} />)}*/}
          <img className={styles.featureImg} alt={'Feature image'} src={Svg}/>
      </div>
      <div className="text--center padding-horiz--md">
        <Heading as="h3">{title}</Heading>
        <p>{description}</p>
      </div>
    </div>
  );
}

export default function HomepageFeatures() {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className="row">
          {FeatureList.map((props, idx) => (
            <Feature key={idx} {...props} />
          ))}
        </div>
      </div>
    </section>
  );
}
