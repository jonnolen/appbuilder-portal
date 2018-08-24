import * as React from 'react';
import { Dropdown } from 'semantic-ui-react';

import { attributesFor } from '@data';

import { IProvidedProps as IDataProps } from './with-data';

interface IOwnProps {
  selected: Id;
  onChange: (groupId: Id) => void;
}

type IProps =
& IOwnProps
& IDataProps;

export default class GroupSelectDisplay extends React.Component<IProps> {
  componentDidMount() {
    const { selected, groups, onChange } = this.props;

    if (!selected && groups && groups.length > 0) {
      const firstId = groups[0].id;

      onChange(firstId);
    }
  }

  onSelect = (e, { value }) => {
    e.preventDefault();

    const { onChange } = this.props;

    onChange(value);
  }

  render() {
    const { groups, selected } = this.props;

    const groupOptions = groups.map(group => ({
      text: attributesFor(group).name,
      value: group.id
    }));

    return (
      <Dropdown
        data-test-group-select
        inline
        options={groupOptions}
        value={selected}
        onChange={this.onSelect}
      />
    );
  }
}